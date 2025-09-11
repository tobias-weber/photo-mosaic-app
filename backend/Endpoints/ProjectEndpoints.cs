using backend.Data;
using backend.DTOs;
using backend.Models;
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

        // POST a new project for a user
        group.MapPost("/", async (AppDbContext db, string userName, string title) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user is null) return Results.NotFound();

            var project = new Project
            {
                ProjectId = Guid.NewGuid(),
                UserId = user.Id,
                Title = title,
            };

            db.Projects.Add(project);
            await db.SaveChangesAsync();
            return Results.Created($"/users/{userName}/projects/{project.ProjectId}", new ProjectDto(project));
        });

        // PUT to update a project
        group.MapPut("/{projectId:guid}",
            async (AppDbContext db, string userName, Guid projectId, Project modifiedProject) =>
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

            db.Projects.Remove(project); // TODO: cascading remove tasks etc.
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static async Task<Project?> GetProjectAsync(AppDbContext db, string userName, Guid projectId)
    {
        return await db.Projects
            .FirstOrDefaultAsync(p => p.User.UserName == userName && p.ProjectId == projectId);
    }
}