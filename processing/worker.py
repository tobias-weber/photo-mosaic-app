import os
import requests
from model import EnqueueJobRequest, JobStatus
from mosaic_creator import *
from PIL import Image
import pyvips
import numpy as np
import json
from datetime import datetime
from typing import Union
import traceback

BASE_PATH = os.getenv("BASE_PATH", "/app/images")
BACKEND_CALLBACK_URL = os.getenv("BACKEND_CALLBACK_URL", "http://host.docker.internal:5243/jobs")
TILE_RESOLUTION = 32
DZ_TILE_RESOLUTION = 512
LOAD_PROGRESS_FRACTION = 0.4  # how much of the progress indicator is used for (typically lazily) loading images
DOWNSCALED_IMAGE_SUFFIX = "sm"

def process_job(request: EnqueueJobRequest):
    try:
        print(f"[{datetime.now().isoformat(sep=' ', timespec='milliseconds')}] Begin processing job {request.job_id}...")
        send_status_update(JobStatus.Processing, request, 0)

        target, tiles = read_images(request)

        if request.algorithm == "LAP":
            builder = init_LAP_builder(target, tiles, request.n, request.subdivisions, request.crop_count, request.repetitions)
        else:
            raise ValueError(f"Unknown algorithm: {request.algorithm}")

        print(f"[{datetime.now().isoformat(sep=' ', timespec='milliseconds')}] Ready to build")
        result = builder.build(lambda p: 
                            send_status_update(JobStatus.Processing, request, LOAD_PROGRESS_FRACTION + (1-LOAD_PROGRESS_FRACTION) * p.progress))

        print(f"[{datetime.now().isoformat(sep=' ', timespec='milliseconds')}] Saving mosaic")
        job_dir = os.path.join(BASE_PATH, "users", request.username, "projects", request.project_id, "mosaics", request.job_id)
        path_list = save_result(request.tiles, result, job_dir)
        send_status_update(JobStatus.GeneratedPreview, request)

        print(f"[{datetime.now().isoformat(sep=' ', timespec='milliseconds')}] Generating deepzoom")
        dz_dir = os.path.join(job_dir, "dz")
        save_deepzoom(path_list, result.shape[1], result.crop_count, dz_dir)
        send_status_update(JobStatus.Finished, request)

        print(f"[{datetime.now().isoformat(sep=' ', timespec='milliseconds')}] Finished processing")
    except Exception:
        traceback.print_exc()
        send_status_update(JobStatus.Failed, request)
        print(f"[{datetime.now().isoformat(sep=' ', timespec='milliseconds')}] Marked job as failed")


def get_image(path, prefer_small=False) -> Image:
    abs_path = os.path.join(BASE_PATH, path)
    if prefer_small:  # drastically speeds up loading of tiles without impact on quality
        downscaled_path = get_downscaled_path(abs_path)
        abs_path = downscaled_path if os.path.exists(downscaled_path) else abs_path

    img = Image.open(abs_path)
    if img.mode != "RGB":
        img = img.convert("RGB")
    return img


def read_images(request: EnqueueJobRequest) -> tuple[Image, list[Image]]:
    target = get_image(request.target)
    
    tiles = []
    for i, t in enumerate(request.tiles):
        tiles.append(get_image(t, prefer_small=True))
        if (i+1) % 50 == 0:
            load_progress = (i+1) / len(request.tiles)
            send_status_update(JobStatus.Processing, request, load_progress * LOAD_PROGRESS_FRACTION)
    return target, tiles


def init_LAP_builder(target: Image, tiles: list[Image], n: int, subdivisions: int, crop_count: int, repetitions: int) -> MosaicBuilder:
    params = {
        'resolution': 32,
        'granularity': subdivisions,
        'crop_count': max(1, crop_count),
        'repetitions': max(1, repetitions),
        'tile_count': max(1, min(n, len(tiles)))
    }
    return MosaicBuilder(photo=target, tile_images=tiles, params=params)


def get_assignment_descriptor(path_list: Union[list[str], list[list[str]]], shape: tuple[int, int], crop_count: int):
    nrows, ncols = shape
    reshaped_paths = [path_list[i*ncols : (i+1)*ncols] for i in range(nrows)]
    return {
        "assignment": reshaped_paths,
        "shape": shape,
        "n": nrows * ncols,
        "crop_count": crop_count
    }


def save_result(tiles: list[str], result: Mosaic, job_dir: str):
    os.makedirs(job_dir, exist_ok=True)
    mosaic_path = os.path.join(job_dir, "mosaic.jpg")
    result.mosaic.save(mosaic_path, format="JPEG")

    path_list = assignment_to_path_list(result.assignment, tiles)
    with open(os.path.join(job_dir, "assignment.json"), 'w') as f:
        descriptor = get_assignment_descriptor(path_list, result.shape, result.crop_count)
        json.dump(descriptor, f)
    return path_list
    

def assignment_to_path_list(assignment: np.ndarray, paths: list[str]) -> Union[list[str], list[list[str]]]:
    if assignment.ndim == 1 or assignment.shape[1] == 1:
        return [paths[i] for i in assignment]  # crop_count = 1
    return [(paths[row[0]], int(row[1])) for row in assignment]  # each assignment becomes a pair (path, crop_idx)


def save_deepzoom(path_list: Union[list[str], list[list[str]]], ncols: int, crop_count: int, dz_dir: str):
    os.makedirs(dz_dir, exist_ok=True)
    tiles = [load_vips_tile(path, crop_count) for path in path_list]
    mosaic = pyvips.Image.arrayjoin(tiles, across=ncols)
    mosaic.dzsave(os.path.join(dz_dir, "dz.jpg"), tile_size=512)


def load_vips_tile(path: Union[str, list[str]], crop_count: int) -> pyvips.Image:
    image_path = path if crop_count == 1 else path[0]

    image = pyvips.Image.new_from_file(os.path.join(BASE_PATH, image_path), access="sequential")
    if image.bands == 4:
        # If RGBA, discard the alpha channel to get RGB
        image = image.extract_band(0, n=3)

    width = image.width
    height = image.height

    # Determine the size of the smaller side for a square crop
    crop_size = min(width, height)

    # Centered crop
    left = (width - crop_size) // 2
    top = (height - crop_size) // 2

    if crop_count > 1:
        # possibly different alignment
        delta_rel = path[1] / (crop_count - 1) - 0.5
        delta_abs = (max(width, height) - crop_size) * delta_rel
        ## square images are translated diagonally
        if width >= height:
            left += delta_abs
        if width <= height:
            top += delta_abs

    # Crop the image to a centered square
    cropped_image = image.extract_area(left, top, crop_size, crop_size)

    # Resize the cropped image to the desired output size
    resized_image = cropped_image.resize(DZ_TILE_RESOLUTION / crop_size)

    return resized_image


def send_status_update(status: JobStatus, request: EnqueueJobRequest, progress: float = None):
    # Notify ASP.NET backend that job is done
    payload = {
        "status": status.value
    }
    if progress is not None:
        payload["progress"] = progress

    try:
        headers = {
            "X-Job-Secret": request.token,
            "Content-Type": "application/json"
            }
        result = requests.post(f"{BACKEND_CALLBACK_URL}/{request.job_id}/status", json=payload, headers=headers)
        if not result.ok:
            print(f"[{datetime.now().isoformat(sep=' ', timespec='milliseconds')}] Request failed. Server response:")
            # Attempt to print the response body, which may contain error details
            try:
                print(f"[{datetime.now().isoformat(sep=' ', timespec='milliseconds')}] Response Body (JSON): {result.json()}")
            except requests.exceptions.JSONDecodeError:
                print(f"[{datetime.now().isoformat(sep=' ', timespec='milliseconds')}] Response Body (Text): {result.text}")

    except Exception as e:
        print(f"[{datetime.now().isoformat(sep=' ', timespec='milliseconds')}] Failed to notify backend: {e}")


def get_downscaled_path(original_path: str) -> str:
    dir_name = os.path.dirname(original_path)
    filename_without_ext = os.path.splitext(os.path.basename(original_path))[0]
    return os.path.join(dir_name, f"{filename_without_ext}_{DOWNSCALED_IMAGE_SUFFIX}.jpg")