import os
from fastapi import FastAPI, HTTPException
from redis import Redis, RedisError
from rq import Queue
from worker import process_job
from model import EnqueueJobRequest

app = FastAPI()
REDIS_HOST = os.getenv("REDIS_HOST", "redis")
REDIS_PORT = int(os.getenv("REDIS_PORT", 6379))
redis_conn = Redis(host=REDIS_HOST, port=REDIS_PORT)
queue = Queue(connection=redis_conn)


@app.post("/enqueue")
async def enqueue_job(request: EnqueueJobRequest):
    try:
        queue.enqueue(process_job, request)
        print(f"Enqueued {request.job_id}")
        return {"status": "enqueued", "job_id": request.job_id}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


@app.get("/healthz")
async def healthz():
    """
    Liveness probe: app is running
    """
    return {"status": "ok"}


@app.get("/readyz")
async def readyz():
    """
    Readiness probe: app can connect to Redis
    """
    try:
        if redis_conn.ping():
            return {"status": "ready"}
        else:
            raise HTTPException(status_code=503, detail="Redis not reachable")
    except RedisError:
        raise HTTPException(status_code=503, detail="Redis not reachable")
