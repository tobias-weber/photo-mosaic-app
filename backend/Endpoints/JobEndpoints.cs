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

        group.MapGet("/", async (string userName, Guid projectId, IProcessingService processing) =>
        {
            try
            {
                return Results.Ok(await processing.GetJobsAsync(userName, projectId));
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        });


        // GET a specific mosaic
        group.MapGet("/{jobId:guid}/mosaic",
            (string userName, Guid projectId, Guid jobId, IImageStorageService storage) =>
            {
                if (!storage.MosaicExists(userName, projectId, jobId))
                {
                    return Results.NotFound();
                }

                try
                {
                    var absPath = storage.GetMosaicPath(userName, projectId, jobId);
                    return Results.File(absPath, "image/jpeg", $"mosaic_{jobId}.jpg");
                }
                catch (FileNotFoundException ex)
                {
                    return Results.InternalServerError(ex.Message);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(ex.Message);
                }
            });

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