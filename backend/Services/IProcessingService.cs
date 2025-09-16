using backend.DTOs;
using backend.Models;

namespace backend.Services;

public interface IProcessingService
{
    Task<Job> EnqueueJobAsync(string userName, Guid projectId, EnqueueJobDto request);

    Task CompleteJobAsync(Guid jobId);

    Task<bool> IsTokenValid(Guid jobId, Guid token);
}