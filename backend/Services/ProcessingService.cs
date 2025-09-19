using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

public class ProcessingService : IProcessingService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _db;
    private readonly IImageStorageService _storage;

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
            N = request.N == 0
                ? await _db.Images.Where(i => i.ProjectId == projectId && !i.IsTarget).CountAsync()
                : request.N,
            Algorithm = request.Algorithm,
            Subdivisions = request.Subdivisions,
            TargetImageId = request.Target
        };

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

    // Called by Python worker to mark a job as complete
    public async Task CompleteJobAsync(Guid jobId)
    {
        var job = await _db.Jobs
            .Include(j => j.Project).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(j => j.JobId == jobId);
        if (job == null)
        {
            throw new InvalidOperationException($"Job {jobId} does not exist.");
        }

        if (_storage.MosaicExists(job.Project.User.UserName!, job.ProjectId, job.JobId))
        {
            Console.WriteLine($"Job {jobId} marked as completed.");
            job.Status = JobStatus.Finished;
            job.FinishedAt = DateTime.UtcNow;
        }
        else
        {
            job.Status = JobStatus.Failed;
            Console.WriteLine($"Job {jobId} marked as failed.");
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