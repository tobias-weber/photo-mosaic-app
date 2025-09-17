using backend.Models;
using Microsoft.AspNetCore.Identity;
using Task = System.Threading.Tasks.Task;

namespace backend.Data;

public class DbInitializer
{
    public static async Task SeedRolesAndAdminAsync(IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var configuration = services.GetRequiredService<IConfiguration>();


        // Create the roles
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>("Admin"));
        }
        if (!await roleManager.RoleExistsAsync("User"))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>("User"));
        }
        
        // Create a default user
        var user = new User { UserName = "user" };
        if (await userManager.FindByNameAsync(user.UserName) == null && configuration["DefaultUser:Password"] is not null)
        {
            await userManager.CreateAsync(user, configuration["DefaultUser:Password"]!);
            await userManager.AddToRoleAsync(user, "User");
        }

        // Create a default admin user
        var adminUser = new User { UserName = "admin" };
        if (await userManager.FindByNameAsync(adminUser.UserName) == null && configuration["Admin:Password"] is not null)
        {
            await userManager.CreateAsync(adminUser, configuration["Admin:Password"]!);
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
        
        
    }
}