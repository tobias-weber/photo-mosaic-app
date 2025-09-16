# Processing (Python)

This service handles job enqueueing and processing for the photo mosaic app.

## Overview

1. UI / ASP.NET backend submits a job to the Python FastAPI service (Endpoint: `POST /enqueue`)

2. Python enqueues the job in Redis.

3. RQ Worker picks up the job:  
    - Processes the mosaic (the storage folder is accessible via Docker volumes)
    - Posts back to backend callback endpoint (`POST /jobs/{jobId}/complete`)
    - Must include the per-job secret in header that was part of the submission: `X-Job-Secret: <job secret>`

## Environment Variables

- REDIS_HOST – Redis hostname (redis in Docker).
- REDIS_PORT – Redis port (6379).
- BACKEND_CALLBACK_URL – URL of ASP.NET backend callback (`http://host.docker.internal:5243/jobs` for dev, `http://backend:8080/jobs` in production).

## Running in Dev (Docker Compose)

Ensure you are in the root directory of the project.

```bash
docker compose -f docker-compose.dev.yml up --build
```

This starts Redis, FastAPI service, and a worker container.

The `/backend/storage` directory is mounted automatically as a volume.

### Scaling Workers

```bash
docker compose -f docker-compose.dev.yml up --scale worker=3
```

Runs 3 worker containers concurrently, processing jobs from the same Redis queue.