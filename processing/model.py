from pydantic import BaseModel
from enum import Enum

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


class JobStatus(Enum):
    Created = 0
    Submitted = 1
    Processing = 2
    GeneratedPreview = 3
    Finished = 4
    Aborted = 5
    Failed = 6
