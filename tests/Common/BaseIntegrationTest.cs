using Infrastructure.Database;
using IntegrationTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IntegrationTests.Common;

public abstract class BaseIntegrationTest : IClassFixture<IntegrationTestWebAppFactory>, IAsyncLifetime
{
    protected readonly IntegrationTestWebAppFactory Factory;
    protected readonly HttpClient HttpClient;
    protected readonly ApplicationDbContext DbContext;
    protected readonly IServiceScope Scope;
    protected readonly Guid TestUserId;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        Factory = factory;
        TestUserId = Guid.CreateVersion7();

        HttpClient = factory.CreateClient();
        AuthenticateAsAdminUser();

        Scope = factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    protected void AuthenticateAsAdminUser()
    {
        HttpClient.DefaultRequestHeaders.Remove("X-Test-UserId");
        HttpClient.DefaultRequestHeaders.Remove("X-Test-Permissions");

        HttpClient.DefaultRequestHeaders.Add("X-Test-UserId", TestUserId.ToString());
        HttpClient.DefaultRequestHeaders.Add("X-Test-Permissions",
            string.Join(",", GetAllPermissions()));
    }

    protected async Task AuthenticateAsUserWithoutPermissionsAsync()
    {
        HttpClient.DefaultRequestHeaders.Remove("X-Test-UserId");
        HttpClient.DefaultRequestHeaders.Remove("X-Test-Permissions");

        HttpClient.DefaultRequestHeaders.Add("X-Test-UserId", Guid.CreateVersion7().ToString());
        await Task.CompletedTask;
    }

    private static List<string> GetAllPermissions()
    {
        return new List<string>
        {
            PermissionRegistry.AdminDashboardRead,
            PermissionRegistry.AdminUserManagementCreate,
            PermissionRegistry.AdminUserManagementRead,
            PermissionRegistry.AdminUserManagementEdit,
            PermissionRegistry.AdminUserManagementDelete,
            PermissionRegistry.AdminSettingsRolePermissionCreate,
            PermissionRegistry.AdminSettingsRolePermissionRead,
            PermissionRegistry.AdminSettingsRolePermissionEdit,
            PermissionRegistry.AdminSettingsRolePermissionDelete,
            PermissionRegistry.PDSetupAccess
        };
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;

    protected virtual async Task CleanupAsync()
    {
        DbContext.ChangeTracker.Clear();

        try
        {
            await DbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"efa_configurations\" RESTART IDENTITY CASCADE;");
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01")
        {
            // Ignore if table does not exist
        }

        try
        {
            await DbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"uploaded_files\" RESTART IDENTITY CASCADE;");
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01")
        {
            // Ignore if table does not exist
        }

        try
        {
            await DbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"branches\" RESTART IDENTITY CASCADE;");
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01")
        {
            // Ignore if table does not exist
        }

        try
        {
            await DbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"organizations\" RESTART IDENTITY CASCADE;");
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01")
        {
            // Ignore if table does not exist
        }
    }

    public virtual async Task DisposeAsync()
    {
        try
        {
            await CleanupAsync();
        }
        catch (Exception)
        {
            // If cleanup fails, the container will be recreated for the next test class
        }
        finally
        {
            await DbContext.DisposeAsync();
            Scope.Dispose();
            HttpClient.Dispose();
        }
    }
}
