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

        // Get a single Job
        group.MapGet("/{jobId:guid}",
            async (string userName, Guid projectId, Guid jobId, IProcessingService processing) =>
            {
                try
                {
                    var job = await processing.GetJobAsync(userName, projectId, jobId);
                    return job == null ? Results.NotFound() : Results.Ok(job);
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

        // get the dzi version of the mosaic
        group.MapGet("/{jobId:guid}/dz/{**filePath}",
            (string userName, Guid projectId, Guid jobId, string filePath, IImageStorageService storage) =>
            {
                if (!storage.DeepZoomExists(userName, projectId, jobId)) // TODO: better check for dz version existence
                {
                    return Results.NotFound();
                }

                try
                {
                    var (absPath, contentType) = storage.GetDeepZoomPath(userName, projectId, jobId, filePath);
                    return Results.File(absPath, contentType);
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


        // Delete a single Job
        group.MapDelete("/{jobId:guid}",
            async (string userName, Guid projectId, Guid jobId, IProcessingService processing) =>
            {
                try
                {
                    await processing.DeleteJobAsync(userName, projectId, jobId);
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });


        // Callbacks used by the processing containers
        var jobCallback = routes.MapGroup("/jobs");

        jobCallback.MapPost("/{jobId:guid}/status",
            async (Guid jobId, JobStatusDto status, HttpRequest request, IProcessingService processing) =>
            {
                if (
                    !request.Headers.TryGetValue("X-Job-Secret", out var headerValue) ||
                    !await processing.IsTokenValid(jobId, Guid.Parse(headerValue!))
                ) return Results.Unauthorized();

                try
                {
                    await processing.UpdateStatus(jobId, status.Status, status.Progress);
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });
    }
}