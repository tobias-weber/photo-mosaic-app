using backend.Data;
using backend.DTOs;
using backend.Models;
using backend.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/users/{userName}/projects")
            .RequireAuthorization("OwnerOrAdmin");

        // GET all projects for a user
        group.MapGet("/", async (AppDbContext db, string userName) =>
            await db.Projects
                .Where(p => p.User.UserName == userName)
                .Select(p => new ProjectDto(p))
                .ToListAsync()
        );

        // GET single project for a user
        group.MapGet("/{projectId:guid}", async (AppDbContext db, string userName, Guid projectId) =>
        {
            var project = await GetProjectAsync(db, userName, projectId);
            return project == null ? Results.NotFound() : Results.Ok(new ProjectDto(project));
        });

        // GET all project imageRefs
        group.MapGet("/{projectId:guid}/images",
            async (string userName, Guid projectId, string? filter, IImageStorageService storage) =>
            {
                try
                {
                    return Results.Ok(await storage.GetImageRefsAsync(userName, projectId, filter));
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(ex.Message);
                }
            });

        // GET a specific image file
        group.MapGet("/{projectId:guid}/images/{imageId:guid}",
            async (string userName, Guid projectId, Guid imageId, IImageStorageService storage) =>
            {
                try
                {
                    var (imageRef, absPath) = await storage.GetImageRefAsync(userName, projectId, imageId);
                    return Results.File(absPath, imageRef.ContentType, imageRef.Name);
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

        // POST a new project for a user
        group.MapPost("/", async (AppDbContext db, string userName, CreateProjectDto request) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user is null) return Results.NotFound();

            var project = new Project
            {
                ProjectId = Guid.NewGuid(),
                UserId = user.Id,
                Title = request.Title,
            };

            db.Projects.Add(project);
            await db.SaveChangesAsync();
            return Results.Created($"/users/{userName}/projects/{project.ProjectId}", new ProjectDto(project));
        });

        // POST an image
        group.MapPost("/{projectId:guid}/images",
            async (string userName, Guid projectId, HttpRequest request, IImageStorageService storage) =>
            {
                var form = await request.ReadFormAsync();

                if (!bool.TryParse(form["isTarget"], out var isTarget))
                {
                    return Results.BadRequest("Invalid or missing isTarget value.");
                }

                try
                {
                    var imageRef =
                        await storage.StoreImageAsync(userName, projectId, isTarget, form.Files.GetFile("file"));
                    return Results.Ok(imageRef.ImageId);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(ex.Message);
                }
            });

        // PUT to update a project
        group.MapPut("/{projectId:guid}",
            async (AppDbContext db, string userName, Guid projectId, ProjectDto modifiedProject) =>
            {
                if (modifiedProject.ProjectId != projectId) return Results.Conflict();
                var project = await GetProjectAsync(db, userName, projectId);
                if (project is null) return Results.NotFound();

                project.Title = modifiedProject.Title;

                await db.SaveChangesAsync();
                return Results.Ok(new ProjectDto(project));
            });

        // DELETE a project
        group.MapDelete("/{projectId:guid}", async (AppDbContext db, string userName, Guid projectId) =>
        {
            var project = await GetProjectAsync(db, userName, projectId);
            if (project is null) return Results.NotFound();

            db.Projects.Remove(project); // TODO: cascading remove jobs etc.
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // DELETE an image
        group.MapDelete("/{projectId:guid}/images/{imageId:guid}",
            async (string userName, Guid projectId, Guid imageId, IImageStorageService storage) =>
            {
                try
                {
                    await storage.DeleteImageAsync(userName, projectId, imageId);
                    return Results.NoContent();
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(ex.Message);
                }
            });

        // DELETE all images
        group.MapDelete("/{projectId:guid}/images",
            async (string userName, Guid projectId, string? filter, IImageStorageService storage) =>
            {
                try
                {
                    await storage.DeleteImagesAsync(userName, projectId, filter);
                    return Results.NoContent();
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(ex.Message);
                }
            });
    }

    private static async Task<Project?> GetProjectAsync(AppDbContext db, string userName, Guid projectId)
    {
        return await db.Projects
            .FirstOrDefaultAsync(p => p.User.UserName == userName && p.ProjectId == projectId);
    }
}