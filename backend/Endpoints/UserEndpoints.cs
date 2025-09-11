using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/users")
            .RequireAuthorization("OwnerOrAdmin");

        // GET all users (for Admins)
        group.MapGet("/", async (AppDbContext db) =>
            {
                var users = await db.Users.Select(u => new UserDto(u)).ToListAsync();
                return Results.Ok(users);
            }
        ).RequireAuthorization("AdminPolicy");

        // GET single user by id
        group.MapGet("/{userName}", async (AppDbContext db, string userName) =>
        {
            var user = await GetUserAsync(db, userName);
            return user is not null ? Results.Ok(new UserDto(user)) : Results.NotFound();
        });

        // DELETE user
        group.MapDelete("/{userName}", async (AppDbContext db, string userName) =>
        {
            var user = await GetUserAsync(db, userName);
            if (user is null) return Results.NotFound();

            db.Users.Remove(user); // TODO: cascading remove projects, tasks etc.
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static async Task<User?> GetUserAsync(AppDbContext db, string userName)
    {
        return await db.Users.Where(u => u.UserName == userName).FirstOrDefaultAsync();
    }
}