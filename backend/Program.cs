using System.Security.Claims;
using System.Text;
using backend.Data;
using backend.Endpoints;
using backend.Helpers;
using backend.Models;
using backend.Services;
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
    .AddPolicy("UserPolicy", policy => policy.RequireRole("User"))
    .AddPolicy("OwnerOrAdmin", policy => policy.RequireAssertion(context =>
        {
            if (context.User.IsInRole("Admin"))
            {
                return true;
            }

            var userNameClaim = context.User.FindFirstValue(ClaimTypes.Name);
            var routeUserName = context.Resource switch
            {
                HttpContext http => http.Request.RouteValues["userName"]?.ToString(),
                _ => null
            };

            return userNameClaim != null && routeUserName == userNameClaim;
        })
    );


// CORS
const string corsPolicy = "_allowFrontend";
var allowedOrigins = new[]
{
    "http://localhost:4200", // Angular dev server
    "http://frontend:80" // Docker Compose service name
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

// Custom services
builder.Services.AddScoped<IImageStorageService, ImageStorageService>();

var app = builder.Build();

// Apply CORS middleware
app.UseCors(corsPolicy); // MUST come before endpoint mapping

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

app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapProjectEndpoints();


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