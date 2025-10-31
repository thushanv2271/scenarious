using Infrastructure.Database;
using Infrastructure.Database.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Web.Api.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();
        using ApplicationDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.Database.Migrate();
    }

    public static async Task SeedDatabaseAsync(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        DatabaseSeeder? seeder = scope.ServiceProvider.GetService<DatabaseSeeder>();

        if (seeder == null)
        {
            ILogger<DatabaseSeeder>? logger = scope.ServiceProvider.GetService<ILogger<DatabaseSeeder>>();
            logger?.LogWarning("DatabaseSeeder not registered. Skipping database seeding.");
            return;
        }

        // Let exceptions propagate - they'll be caught by the global exception handler
        await seeder.SeedAsync();
    }
}
