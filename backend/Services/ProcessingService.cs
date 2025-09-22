using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class ProcessingService : IProcessingService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _db;
    private readonly IImageStorageService _storage;

    private const int MaxN = 4000;

    public ProcessingService(HttpClient httpClient, AppDbContext db, IImageStorageService storage)
    {
        _httpClient = httpClient;
        _db = db;
        _storage = storage;
    }

    public async Task<Job> EnqueueJobAsync(string userName, Guid projectId, EnqueueJobDto request)
    {
        await CheckIfProjectValid(userName, projectId);

        var job = new Job()
        {
            JobId = Guid.NewGuid(),
            Token = Guid.NewGuid(),
            ProjectId = projectId,
            Status = JobStatus.Created,
            Progress = 0,
            N = request.N == 0
                ? await _db.Images.Where(i => i.ProjectId == projectId && !i.IsTarget).CountAsync()
                : request.N,
            Algorithm = request.Algorithm,
            Subdivisions = request.Subdivisions,
            TargetImageId = request.Target
        };
        if (job.N > MaxN)
        {
            throw new InvalidOperationException($"N is currently limited to at most {MaxN} tiles.");
        }

        var targetPath = await _db.Images.Where(i => i.ImageId == job.TargetImageId && i.IsTarget)
            .Select(i => i.FilePath).FirstOrDefaultAsync();
        if (targetPath is null)
        {
            throw new InvalidOperationException($"The target image {job.TargetImageId} does not exist.");
        }

        var tilePaths = await _db.Images.Where(i => i.ProjectId == projectId && !i.IsTarget).Select(i => i.FilePath)
            .ToListAsync();

        var payload = new
        {
            job_id = job.JobId,
            username = userName,
            project_id = projectId,
            token = job.Token,
            n = job.N,
            algorithm = job.Algorithm,
            subdivisions = job.Subdivisions,
            target = targetPath,
            tiles = tilePaths
        };

        _db.Jobs.Add(job);
        await _db.SaveChangesAsync();

        var response = await _httpClient.PostAsJsonAsync("/enqueue", payload);

        if (!response.IsSuccessStatusCode) throw new HttpRequestException(await response.Content.ReadAsStringAsync());

        job.Status = JobStatus.Submitted;
        await _db.SaveChangesAsync();
        return job;
    }

    public async Task<List<JobDto>> GetJobsAsync(string userName, Guid projectId)
    {
        await CheckIfProjectValid(userName, projectId);
        return await _db.Jobs
            .Where(j => j.ProjectId == projectId)
            .Select(j => new JobDto(j))
            .ToListAsync();
    }

    // Called by Python worker to update status
    public async Task UpdateStatus(Guid jobId, JobStatus status, double? progress)
    {
        var job = await _db.Jobs.FindAsync(jobId);
        if (job is null) throw new InvalidOperationException($"Job {jobId} does not exist.");

        if (status < job.Status || status != job.Status &&
            job.Status is JobStatus.Finished or JobStatus.Failed or JobStatus.Aborted)
            throw new InvalidOperationException(
                $"Cannot move from status '{job.Status}' to status '{status}'.");

        switch (status)
        {
            case JobStatus.Processing:
                if (progress is null)
                    throw new InvalidOperationException($"Cannot externally update JobStatus to {status}.");
                job.Status = JobStatus.Processing;
                job.Progress = double.Clamp(progress.Value, 0, 1);
                break;

            case JobStatus.GeneratedPreview:
                // Custom logic for a finished job
                var userName = await _db.Jobs
                    .Where(j => j.JobId == jobId)
                    .Select(j => j.Project.User.UserName!)
                    .FirstAsync();
                if (_storage.MosaicExists(userName, job.ProjectId, job.JobId))
                {
                    Console.WriteLine($"Job {jobId} marked as completed.");
                    job.Status = JobStatus.GeneratedPreview;
                }
                else
                {
                    job.Status = JobStatus.Failed;
                    Console.WriteLine($"Job {jobId} marked as failed.");
                }

                break;

            case JobStatus.Finished:
                job.Status = JobStatus.Finished;
                job.FinishedAt = DateTime.UtcNow;
                break;

            case JobStatus.Submitted:
            case JobStatus.Created:
            case JobStatus.Aborted:
                throw new InvalidOperationException($"Cannot externally update JobStatus to {status}.");

            case JobStatus.Failed: // TODO: cleanup after failure
            default:
                job.Status = status;
                break;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<JobDto?> GetJobAsync(string userName, Guid projectId, Guid jobId)
    {
        await CheckIfProjectValid(userName, projectId);
        return await _db.Jobs
            .Where(j => j.JobId == jobId && j.ProjectId == projectId)
            .Select(j => new JobDto(j))
            .FirstOrDefaultAsync();
    }

    public async Task DeleteJobAsync(string userName, Guid projectId, Guid jobId)
    {
        await CheckIfProjectValid(userName, projectId);
        var job = await _db.Jobs.FirstOrDefaultAsync(j => j.JobId == jobId && j.ProjectId == projectId);
        if (job is null) throw new InvalidOperationException($"Job {jobId} does not exist.");
        if (job.Status != JobStatus.Finished && job.Status != JobStatus.Aborted && job.Status != JobStatus.Failed) 
            throw new InvalidOperationException($"Job {jobId} is still active.");
        
        _storage.DeleteMosaic(userName, job.ProjectId, jobId);
        _db.Jobs.Remove(job);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> IsTokenValid(Guid jobId, Guid token)
    {
        var job = await _db.Jobs.FindAsync(jobId);
        return job != null && job.Token == token;
    }

    private async Task CheckIfProjectValid(string userName, Guid projectId)
    {
        var userHasProject = await _db.Projects.AnyAsync(p => p.ProjectId == projectId && p.User.UserName == userName);
        if (!userHasProject)
        {
            throw new InvalidOperationException($"Specified project for user '{userName}' does not exist.");
        }
    }
}