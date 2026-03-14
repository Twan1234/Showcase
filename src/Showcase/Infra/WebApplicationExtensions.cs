using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Showcase.Areas.Identity.Data;
using Showcase.Data;

namespace Showcase.Infra;

public static class WebApplicationExtensions
{
    private static readonly string[] ConnectionStringKeys = { "AuthDbContextConnection", "TicTacToeDbConnection" };
    private static readonly string[] DefaultRoles = { "Admin", "Member" };

    public static void EnsureSqliteDirectories(this WebApplication app)
    {
        var config = app.Services.GetRequiredService<IConfiguration>();
        if (!StorageServiceCollectionExtensions.IsUsingSqlite(config))
            return;

        const string dataSourcePrefix = "Data Source=";

        foreach (var key in ConnectionStringKeys)
        {
            var connectionString = config.GetConnectionString(key);
            if (string.IsNullOrWhiteSpace(connectionString))
                continue;

            var prefixIndex = connectionString.IndexOf(dataSourcePrefix, StringComparison.OrdinalIgnoreCase);
            if (prefixIndex < 0)
                continue;

            var path = connectionString.Substring(prefixIndex + dataSourcePrefix.Length).Trim();
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }
    }

    public static void RunMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        authDb.Database.Migrate();
    }

    public static async Task SeedRolesAndAdminAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in DefaultRoles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminEmail = app.Configuration["AdminEmail"] ?? "twan.kloek@gmail.com";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin != null && !await userManager.IsInRoleAsync(admin, "Admin"))
            await userManager.AddToRoleAsync(admin, "Admin");
    }
}
