import os
from fastapi import FastAPI, HTTPException
from redis import Redis
from rq import Queue
from worker import process_job
from model import EnqueueJobRequest

app = FastAPI()
redis_conn = Redis(host=os.getenv("REDIS_HOST", "redis"), port=int(os.getenv("REDIS_PORT", 6379)))
queue = Queue(connection=redis_conn)

BACKEND_CALLBACK_URL = os.getenv("BACKEND_CALLBACK_URL", "http://host.docker.internal:5243/jobs")


@app.post("/enqueue")
async def enqueue_job(request: EnqueueJobRequest):
    try:
        print(f"received request: {request}")
        queue.enqueue(process_job, request, BACKEND_CALLBACK_URL)
        return {"status": "enqueued", "job_id": request.job_id}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
