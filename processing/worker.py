import os
import requests
from model import EnqueueJobRequest, JobStatus
from mosaic_creator import *
from PIL import Image
import pyvips
import numpy as np
import json
from datetime import datetime

BASE_PATH = os.getenv("BASE_PATH", "/app/images")
BACKEND_CALLBACK_URL = os.getenv("BACKEND_CALLBACK_URL", "http://host.docker.internal:5243/jobs")
TILE_RESOLUTION = 32
DZ_TILE_RESOLUTION = 512
LOAD_PROGRESS_FRACTION = 0.4  # how much of the progress indicator is used for (typically lazily) loading images
DOWNSCALED_IMAGE_SUFFIX = "sm"

def process_job(request: EnqueueJobRequest):
    # TODO: Catch exceptions and mark job as failed
    print(f"[{datetime.now().isoformat(sep=' ', timespec='milliseconds')}] Begin processing job {request.job_id}...")
    send_status_update(JobStatus.Processing, request, 0)

    target, tiles = read_images(request)

    if request.algorithm == "LAP":
        builder = init_LAP_builder(target, tiles, request.n, request.subdivisions)
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
    save_deepzoom(path_list, result.shape[1], dz_dir)
    send_status_update(JobStatus.Finished, request)

    print(f"[{datetime.now().isoformat(sep=' ', timespec='milliseconds')}] Finished processing")


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


def init_LAP_builder(target, tiles, n, subdivisions):
    params = {
        'resolution': 32,
        'granularity': subdivisions,
        'repetitions': 1,
        'crop_count': 1,
        'tile_count': max(1, min(n, len(tiles)))
    }
    return MosaicBuilder(photo=target, tile_images=tiles, params=params)


def get_assignment_descriptor(path_list, shape):
    nrows, ncols = shape
    reshaped_paths = [path_list[i*ncols : (i+1)*ncols] for i in range(nrows)]
    return {
        "assignment": reshaped_paths,
        "shape": shape,
        "n": nrows * ncols
    }


def save_result(tiles, result, job_dir):
    os.makedirs(job_dir, exist_ok=True)
    mosaic_path = os.path.join(job_dir, "mosaic.jpg")
    result.mosaic.save(mosaic_path, format="JPEG")

    path_list = assignment_to_path_list(result.assignment, tiles)
    with open(os.path.join(job_dir, "assignment.json"), 'w') as f:
        descriptor = get_assignment_descriptor(path_list, result.shape)
        json.dump(descriptor, f, indent=2)
    return path_list
    

def assignment_to_path_list(assignment: np.ndarray, paths: list[str]) -> list[str]:
    return [paths[i] for i in assignment]


def save_deepzoom(path_list, ncols, dz_dir):
    os.makedirs(dz_dir, exist_ok=True)
    tiles = [load_vips_tile(path) for path in path_list]
    mosaic = pyvips.Image.arrayjoin(tiles, across=ncols)
    mosaic.dzsave(os.path.join(dz_dir, "dz.jpg"), tile_size=512)


def load_vips_tile(path: str) -> pyvips.Image:
    image = pyvips.Image.new_from_file(os.path.join(BASE_PATH, path), access="sequential")
    if image.bands == 4:
        # If RGBA, discard the alpha channel to get RGB
        image = image.extract_band(0, n=3)

    width = image.width
    height = image.height

    # Determine the size of the smaller side for a square crop
    crop_size = min(width, height)

    # Calculate the top-left coordinates for the centered crop
    left = (width - crop_size) // 2
    top = (height - crop_size) // 2

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