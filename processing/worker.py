import os
import time
import requests
from model import EnqueueJobRequest
from mosaic_creator import *
from PIL import Image


BASE_PATH = os.getenv("BASE_PATH", "/app/images")
TILE_RESOLUTION = 32

def process_job(request: EnqueueJobRequest, callback_url: str):
    print(f"Processing job {request.job_id}...")
    # Simulate mosaic processing

    target, tiles = read_images(request.target, request.tiles)

    if request.algorithm == "LAP":
        builder = init_LAP_builder(target, tiles, request.n, request.subdivisions)
    else:
        raise ValueError(f"Unknown algorithm: {request.algorithm}")

    print("Ready to build")
    result = builder.build(progress_callback)

    print("Saving")
    save_result(request.username, request.project_id, result, request.job_id)


    print(f"Finished processing")

    # Notify ASP.NET backend that job is done
    try:
        headers = {"X-Job-Secret": request.token}
        result = requests.post(f"{callback_url}/{request.job_id}/complete", headers=headers)
        print(f"Job {request.job_id} completed and backend notified: {result.status_code}")
    except Exception as e:
        print(f"Failed to notify backend: {e}")


def get_image(path) -> Image:
    img = Image.open(os.path.join(BASE_PATH, path)).convert("RGB")
     # make shorter side TILE_RESOLUTION pixels long
    s_max = max(img.size) / min(img.size) * TILE_RESOLUTION
    img.thumbnail((s_max, s_max), Image.LANCZOS)
    return img


def read_images(targetPath, tilePaths) -> tuple[Image, list[Image]]:
    target = get_image(targetPath)
    tiles = [get_image(t) for t in tilePaths]
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


def save_result(username, project_id, result, job_id):
    job_dir = os.path.join(BASE_PATH, "users", username, "projects", project_id, "mosaics", job_id)
    os.makedirs(job_dir, exist_ok=True)
    mosaic_path = os.path.join(job_dir, "mosaic.jpg")
    result.mosaic.save(mosaic_path, format="JPEG")


def progress_callback(i):
    print(f'Progress: {i}')
