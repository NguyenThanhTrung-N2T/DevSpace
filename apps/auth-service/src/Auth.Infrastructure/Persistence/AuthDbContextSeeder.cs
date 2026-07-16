using Auth.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Auth.Infrastructure.Persistence;

public static class AuthDbContextSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AuthDbContext>>();

        try
        {
            logger.LogInformation("Applying migrations to PostgreSQL database...");
            await context.Database.MigrateAsync();

            logger.LogInformation("Seeding database roles...");
            var roles = new[] { "Admin", "User" };
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new Role(roleName));
                }
            }

            logger.LogInformation("Seeding default Administrator user...");
            var adminEmail = "admin@devspace.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    DisplayName = "Administrator",
                    EmailConfirmed = true,
                    EmailVerified = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    logger.LogInformation("Successfully seeded Admin user.");
                }
                else
                {
                    logger.LogError("Failed to seed Admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating or seeding the database.");
        }
    }
}
