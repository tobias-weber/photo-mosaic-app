using backend.Models;

namespace backend.DTOs;

public class JobStatusDto
{
    public JobStatus Status { get; set; }
    public double? Progress { get; set; }
}