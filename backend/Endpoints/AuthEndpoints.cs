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
        routes.MapPost("/register", async (RegisterDto request, UserManager<User> userManager, IConfiguration config) =>
        {
            var user = new User { UserName = request.UserName };
            var result = await userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded) return Results.BadRequest(result.Errors);
            
            Console.WriteLine($"Config: {config["Jwt:Issuer"]} {config["Jwt:Audience"]} {config["Jwt:Key"]} {config["Jwt:ExpiresInHours"]}");
            await userManager.AddToRoleAsync(user, "User");
            var authResult = await AuthHelper.GenerateTokenAsync(user!, config, userManager);
            return Results.Ok(authResult);
        });

        // Login Endpoint
        routes.MapPost("/login", async (LoginDto request, SignInManager<User> signInManager, IConfiguration config, UserManager<User> userManager) =>
        {
            var result = await signInManager.PasswordSignInAsync(request.UserName, request.Password, isPersistent: false, lockoutOnFailure: false);

            if (!result.Succeeded) return Results.Unauthorized();
            
            var user = await userManager.FindByNameAsync(request.UserName);
            var authResult = await AuthHelper.GenerateTokenAsync(user!, config, userManager);
            return Results.Ok(authResult);
        });
    }
    
}