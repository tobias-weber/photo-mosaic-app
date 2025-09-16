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

    public ProcessingService(HttpClient httpClient, AppDbContext db)
    {
        _httpClient = httpClient;
        _db = db;
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
            N = request.N,
            Algorithm = request.Algorithm,
            Subdivisions = request.Subdivisions,
            TargetImageId = request.Target
        };
        _db.Jobs.Add(job);
        await _db.SaveChangesAsync();
        
        var payload = new
        {
            job_id = job.JobId,
            username = userName,
            project_id = projectId,
            token = job.Token,
            n = job.N,
            algorithm = job.Algorithm,
            subdivisions = job.Subdivisions,
            target = job.TargetImageId
        };
        var response = await _httpClient.PostAsJsonAsync("/enqueue", payload);

        if (!response.IsSuccessStatusCode) throw new HttpRequestException(await response.Content.ReadAsStringAsync());
        
        job.Status = JobStatus.Submitted;
        await _db.SaveChangesAsync();
        return job;
    }

    // Called by Python worker to mark a job as complete
    public async Task CompleteJobAsync(Guid jobId)
    {
        var job = await _db.Jobs.FindAsync(jobId);
        if (job == null)
        {
            throw new InvalidOperationException($"Job {jobId} does not exist.");
        }
        Console.WriteLine($"Job {jobId} marked as completed.");
        job.Status = JobStatus.Finished;
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
