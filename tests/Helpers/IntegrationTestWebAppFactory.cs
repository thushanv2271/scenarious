using Infrastructure.Database;
using Infrastructure.Database.Seeding;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Web.Api;
using Xunit;

namespace IntegrationTests.Helpers;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .WithDatabase("test_db")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithPortBinding(5432, true) // Use random port
        .WithCommand("-c", "max_connections=200") // Increase max connections
        .WithCommand("-c", "shared_buffers=256MB") // Better performance
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            // Add test database context with connection pooling
            services.AddDbContext<ApplicationDbContext>((sp, options) => options.UseNpgsql(_dbContainer.GetConnectionString(), npgsqlOptions => npgsqlOptions.CommandTimeout(60))
                .UseSnakeCaseNamingConvention()
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors());

            // Remove database seeder to prevent conflicts in tests
            services.RemoveAll<DatabaseSeeder>();

            // Add test authentication
            services.AddAuthentication("Test")
                .AddScheme<TestAuthenticationSchemeOptions, TestAuthenticationHandler>(
                    "Test", options => { });

            // Override the default authentication scheme
            services.Configure<Microsoft.AspNetCore.Authentication.AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            });
        });

        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        // Start the container with a longer timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await _dbContainer.StartAsync(cts.Token);

        // Wait for PostgreSQL to be fully ready
        await WaitForDatabaseReadyAsync();

        // Create scope and initialize database
        using IServiceScope scope = Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Ensure database is created with retry logic
        int retryCount = 0;
        const int maxRetries = 5;

        while (retryCount < maxRetries)
        {
            try
            {
                await dbContext.Database.EnsureCreatedAsync();
                break;
            }
            catch (Exception) when (retryCount < maxRetries - 1)
            {
                retryCount++;
                await Task.Delay(TimeSpan.FromSeconds(2 * retryCount)); // Exponential backoff
            }
        }

        // Seed only essential data (permissions) for tests
        await SeedTestDataAsync(dbContext);
    }

    private async Task WaitForDatabaseReadyAsync()
    {
        const int maxAttempts = 30;
        const int delayMilliseconds = 1000;

        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                using IServiceScope scope = Services.CreateScope();
                ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                if (await dbContext.Database.CanConnectAsync())
                {
                    return;
                }
            }
            catch
            {
                // Database not ready yet
            }

            await Task.Delay(delayMilliseconds);
        }

        throw new TimeoutException("Database failed to become ready within the expected time.");
    }

    private static async Task SeedTestDataAsync(ApplicationDbContext context)
    {
        // Seed permissions only (needed for authorization tests)
        HashSet<string> existingPermissions = await context.Permissions
            .Select(p => p.Key)
            .ToHashSetAsync();

        var permissionsToAdd = SharedKernel.PermissionRegistry.GetAllPermissions()
            .Where(permissionDef => !existingPermissions.Contains(permissionDef.Key))
            .Select(permissionDef => new Domain.Permissions.Permission(Guid.CreateVersion7(), permissionDef.Key, permissionDef.DisplayName, permissionDef.Category, permissionDef.Description))
            .ToList();

        if (permissionsToAdd.Count > 0)
        {
            context.Permissions.AddRange(permissionsToAdd);
            await context.SaveChangesAsync();
        }
    }

    public new async Task DisposeAsync()
    {
        try
        {
            await _dbContainer.StopAsync();
        }
        finally
        {
            await _dbContainer.DisposeAsync();
        }
    }
}
