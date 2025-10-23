using backend.Data;
using backend.DTOs;
using backend.Helpers;
using backend.Models;
using Microsoft.AspNetCore.Identity;

namespace backend.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        // Registration Endpoint
        routes.MapPost("/register",
            async (RegisterDto request, UserManager<User> userManager, IConfiguration config, HttpContext httpContext,
                AppDbContext db) =>
            {
                var user = new User { UserName = request.UserName };
                var result = await userManager.CreateAsync(user, request.Password);

                if (!result.Succeeded) return Results.BadRequest(result.Errors);

                await userManager.AddToRoleAsync(user, "User");
                var authResult = await AuthHelper.GenerateTokenAsync(user!, config, userManager);
                var refreshToken = await AuthHelper.AddRefreshTokenAsync(user, httpContext, config, db);
                AuthHelper.SetRefreshCookie(httpContext, refreshToken);

                return Results.Ok(authResult);
            });

        // Login Endpoint
        routes.MapPost("/login",
            async (LoginDto request, SignInManager<User> signInManager, IConfiguration config,
                UserManager<User> userManager, AppDbContext db, HttpContext httpContext) =>
            {
                if (request.UserName != DbInitializer.GuestUserName)
                {
                    var result = await signInManager.PasswordSignInAsync(request.UserName, request.Password,
                        isPersistent: false, lockoutOnFailure: false);
                    if (!result.Succeeded) return Results.Unauthorized();
                }

                var user = (await userManager.FindByNameAsync(request.UserName))!;
                var authResult = await AuthHelper.GenerateTokenAsync(user, config, userManager);
                var refreshToken = await AuthHelper.AddRefreshTokenAsync(user, httpContext, config, db);
                AuthHelper.SetRefreshCookie(httpContext, refreshToken);

                return Results.Ok(authResult);
            });

        // Refresh Endpoint
        routes.MapPost("/refresh", async (
            AppDbContext db,
            IConfiguration config,
            UserManager<User> userManager,
            HttpContext httpContext) =>
        {
            var existingToken = await AuthHelper.GetValidRefreshTokenAsync(httpContext, db);
            if (existingToken is null)
            {
                return Results.Unauthorized();
            }

            existingToken.IsRevoked = true; // db is saved when new token is generated

            var authResult = await AuthHelper.GenerateTokenAsync(existingToken.User, config, userManager);
            var refreshToken = await AuthHelper.AddRefreshTokenAsync(existingToken.User, httpContext, config, db);
            AuthHelper.SetRefreshCookie(httpContext, refreshToken);

            return Results.Ok(authResult);
        });


        routes.MapPost("/logout", async (HttpContext httpContext, AppDbContext db) =>
        {
            // If not logged in, Results.Ok() is also returned
            await AuthHelper.RevokeRefreshTokenAndCookieAsync(httpContext, db);
            return Results.Ok();
        });
    }
}