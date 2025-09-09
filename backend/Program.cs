using System.Text;
using backend.Data;
using backend.DTOs;
using backend.Helpers;
using backend.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Database service
builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));


// Authentication
builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 6;
        options.Password.RequiredUniqueChars = 1;

        // Configure UserName as the unique identifier.
        options.User.RequireUniqueEmail = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Add authentication and configure JWT Bearer
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"))
    .AddPolicy("UserPolicy", policy => policy.RequireRole("User"));




// CORS
var  corsPolicy = "_allowFrontend";
var allowedOrigins = new string[]
{
    "http://localhost:4200",   // Angular dev server
    "http://frontend:80"       // Docker Compose service name
};
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Apply CORS middleware
app.UseCors(corsPolicy);  // MUST come before endpoint mapping

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Create the database if it does not exist
using (var scope = app.Services.CreateScope())
{
    DatabaseHelper.EnsureDatabaseAndDirectoryCreated(scope.ServiceProvider.GetRequiredService<AppDbContext>());
}

// Seed the database with roles and a default admin user
await DbInitializer.SeedRolesAndAdminAsync(app);

// Add the authentication middleware
app.UseAuthentication();
app.UseAuthorization();



//////////////////// Endpoints ///////////////////////

// Registration Endpoint
app.MapPost("/register", async (RegisterModel model, UserManager<User> userManager, IConfiguration config) =>
{
    var user = new User { UserName = model.UserName };
    var result = await userManager.CreateAsync(user, model.Password);

    if (result.Succeeded)
    {
        Console.WriteLine($"Config: {config["Jwt:Issuer"]} {config["Jwt:Audience"]} {config["Jwt:Key"]} {config["Jwt:ExpiresInHours"]}");
        await userManager.AddToRoleAsync(user, "User");
        var authResult = await AuthHelper.GenerateTokenAsync(user!, config, userManager);
        return Results.Ok(authResult);
    }

    return Results.BadRequest(result.Errors);
});

// Login Endpoint
app.MapPost("/login", async (LoginModel model, SignInManager<User> signInManager, IConfiguration config, UserManager<User> userManager) =>
{
    var result = await signInManager.PasswordSignInAsync(model.UserName, model.Password, isPersistent: false, lockoutOnFailure: false);

    if (result.Succeeded)
    {
        var user = await userManager.FindByNameAsync(model.UserName);
        var authResult = await AuthHelper.GenerateTokenAsync(user!, config, userManager);
        return Results.Ok(authResult);
    }

    return Results.Unauthorized();
});


//////////////////// For testing ///////////////////////

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

app.MapGet("/admin-dashboard", () => "Welcome to the admin dashboard!")
    .RequireAuthorization("AdminPolicy");

app.MapGet("/user-data", () => "Here is some user-specific data.")
    .RequireAuthorization("UserPolicy");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}