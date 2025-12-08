namespace Saral.FileProcessor.Data.Extensions;

/// <summary>
/// Extension methods for configuring SaralFileProcessor database services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SaralFileProcessor database services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">The PostgreSQL connection string</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddSaralFileProcessorDatabase(this IServiceCollection services, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string cannot be null or whitespace", nameof(connectionString));
        }

        // Add Entity Framework DbContext
        services.AddDbContext<FileProcessorDbContext>(options => options.UseNpgsql(connectionString));

        // Register database services
        services.AddScoped<IFileProcessingResultService, FileProcessingResultService>();

        return services;
    }

    /// <summary>
    /// Adds SaralFileProcessor database services with configuration options
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">The PostgreSQL connection string</param>
    /// <param name="configureDbContext">Action to configure DbContext options</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddSaralFileProcessorDatabase(
        this IServiceCollection services,
        string connectionString,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string cannot be null or whitespace", nameof(connectionString));
        }

        // Add Entity Framework DbContext with custom configuration
        services.AddDbContext<FileProcessorDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            configureDbContext?.Invoke(options);
        });

        // Register database services
        services.AddScoped<IFileProcessingResultService, FileProcessingResultService>();

        return services;
    }
}
