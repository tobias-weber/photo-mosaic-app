using backend.Models;

namespace backend.DTOs;

public class JobDto(Job job)
{
    public Guid JobId { get; init; } = job.JobId;
    public DateTime StartedAt { get; set; } = job.StartedAt;
    public DateTime FinishedAt { get; set; } = job.FinishedAt;
    public JobStatus Status { get; set; } = job.Status;
    public double Progress { get; set; } = job.Progress;
    public int N { get; set; } = job.N;
    public string Algorithm { get; set; } = job.Algorithm;
    public string ColorSpace { get; set; } = job.ColorSpace;
    public int Subdivisions { get; set; } = job.Subdivisions;
    public int CropCount { get; set; } = job.CropCount;
    public int Repetitions { get; set; } = job.Repetitions;
    public Guid Target { get; set; } = job.TargetImageId;
}