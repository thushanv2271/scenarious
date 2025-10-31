using Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Infrastructure;

public abstract class BaseIntegrationTest : IClassFixture<IntegrationTestWebAppFactory>, IDisposable
{
    protected readonly HttpClient HttpClient;
    protected readonly ApplicationDbContext DbContext;
    private readonly IServiceScope _scope;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        _scope = factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        HttpClient = factory.CreateClient();

        // Ensure database is created and clean
        DbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        // Clean up after each test
        DbContext.Database.EnsureDeleted();
        _scope.Dispose();
        GC.SuppressFinalize(this);
    }
}
