import time
import requests
from model import EnqueueJobRequest

def process_job(request: EnqueueJobRequest, callback_url: str):
    print(f"Processing job {request.job_id}...")
    # Simulate mosaic processing
    time.sleep(5)
    print(f"Processed job: {request}")

    # Notify ASP.NET backend that job is done
    try:
        headers = {"X-Job-Secret": request.token}
        result = requests.post(f"{callback_url}/{request.job_id}/complete", headers=headers)
        print(f"Job {request.job_id} completed and backend notified: {result.status_code}")
    except Exception as e:
        print(f"Failed to notify backend: {e}")
