using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Identity;
using Task = System.Threading.Tasks.Task;

namespace backend.Data;

public class DbInitializer
{
    public const string GuestUserName = "guest";
    private const string GuestUserPassword = "guestUser"; // no pw is required for login

    public static async Task SeedRolesAndUsersAsync(IHost host)
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
        
        if (!await roleManager.RoleExistsAsync("Guest"))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>("Guest"));
        }

        // Create a guest user with no password required
        var guestUser = new User { UserName = GuestUserName };
        if (await userManager.FindByNameAsync(guestUser.UserName) == null)
        {
            await userManager.CreateAsync(guestUser, GuestUserPassword);
            await userManager.AddToRoleAsync(guestUser, "Guest");
        }

        // Create a default admin user
        var adminUser = new User { UserName = "admin" };
        if (await userManager.FindByNameAsync(adminUser.UserName) == null &&
            configuration["Admin:Password"] is not null)
        {
            await userManager.CreateAsync(adminUser, configuration["Admin:Password"]!);
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }

    public static async Task InitTileCollections(IHost host)
    {
        using var scope = host.Services.CreateScope();
        var collections = scope.ServiceProvider.GetRequiredService<ITileCollectionService>();
        await collections.InitTileCollectionsAsync();
    }
}