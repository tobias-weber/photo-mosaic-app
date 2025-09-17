from pydantic import BaseModel

class EnqueueJobRequest(BaseModel):
    job_id: str
    username: str
    project_id: str
    token: str
    n: int
    algorithm: str
    subdivisions: int
    target: str
    tiles: list[str]
