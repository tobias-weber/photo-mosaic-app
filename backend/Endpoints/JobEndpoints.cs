using backend.Data;
using backend.DTOs;
using backend.Services;

namespace backend.Endpoints;

public static class JobEndpoints
{
    public static void MapJobEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/users/{userName}/projects/{projectId:guid}/jobs")
            .RequireAuthorization("OwnerOrAdmin");

        group.MapPost("/",
            async (string userName, Guid projectId, EnqueueJobDto request, AppDbContext db,
                IProcessingService processing) =>
            {
                try
                {
                    var result = await processing.EnqueueJobAsync(userName, projectId, request);
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });


        
        // Callbacks used by the processing containers
        var jobCallback = routes.MapGroup("/jobs");

        jobCallback.MapPost("/{jobId:guid}/complete",
            async (Guid jobId, HttpRequest request, AppDbContext db, IProcessingService processing) =>
            {
                if (
                    !request.Headers.TryGetValue("X-Job-Secret", out var headerValue) ||
                    !await processing.IsTokenValid(jobId, Guid.Parse(headerValue!))
                ) return Results.Unauthorized();

                await processing.CompleteJobAsync(jobId);
                Console.WriteLine($"Successful callback from {jobId}");
                return Results.Ok();
            });
    }
}