using System.Text;
using Application.Abstractions.Authentication;
using Application.Abstractions.Calculations;
using Application.Abstractions.Configuration;
using Application.Abstractions.Data;
using Application.Abstractions.Exporting;
using Application.Abstractions.Parsing;
using Application.Abstractions.Services;
using Application.Abstractions.Pipeline;
using Application.Abstractions.Storage;
using Application.FacilityCashFlowTypes.SaveCashFlowType.Validators;
using Application.IndividualImpairment.Services;
using Application.LGD.Services;
using Application.PD.Services;
using Application.ProductCategories;
using Infrastructure.Authentication;
using Infrastructure.Authorization;
using Infrastructure.Configuration;
using Infrastructure.Database;
using Infrastructure.Database.Seeding;
using Infrastructure.DomainEvents;
using Infrastructure.Exporting;
using Infrastructure.LGD;
using Infrastructure.PD;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.Pipeline;
using Infrastructure.Storage;
using Infrastructure.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using SharedKernel;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services
            .AddServices(configuration)
            .AddRedis(configuration)
            .AddDatabase(configuration)
            .AddHealthChecks(configuration)
            .AddAuthenticationInternal(configuration)
            .AddAuthorizationInternal();

    private static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddTransient<IDomainEventsDispatcher, DomainEventsDispatcher>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddSingleton<Application.Abstractions.Caching.IEclThresholdSummaryCache, Infrastructure.Caching.EclThresholdSummaryCache>();
        var appConfiguration = new AppConfiguration(configuration);
        services.AddSingleton<IAppConfiguration>(appConfiguration);


        if (appConfiguration.HostingType == "Cloud")
        {
            // Azure Blob (example)
            services.AddScoped<IStorageService>(sp =>
            {
                string? blobConnectionString = configuration.GetConnectionString("BlobStorage");
                string? containerName = configuration["Blob:Container"];
                var containerClient = new Azure.Storage.Blobs.BlobContainerClient(blobConnectionString!, containerName!);
                return new BlobStorageService(containerClient);
            });
        }
        else
        {
            // Local File Storage
            services.AddScoped<IStorageService>(_ =>
                new FileStorageService(appConfiguration.UserExportPath));
        }

        services.AddScoped(typeof(IExportService<>), typeof(ExcelExportService<>));

        // Register PD Calculation service as transient
        services.AddTransient<IPDCalculationService, PDCalculationService>();

        // Register LGD Calculation service as transient
        services.AddTransient<ILgdCalculationService, LgdCalculationService>();
        // Register PD Pipeline orchestration service
        services.AddTransient<IPDPipelineService, PDPipelineService>();
        // Register LGD Pipeline orchestration service
        services.AddTransient<ILgdPipelineService, LgdPipelineService>();

        // CSV Services
        services.AddScoped<ICsvParsingService, Services.CsvParsingService>();

        // Register Excel Cash Flow Parser
        services.AddScoped<IExcelCashFlowParser, ExcelCashFlowParser>();

        // Saral.FileProcessor infrastructure services
        services.AddScoped<Saral.FileProcessor.Core.Abstractions.IFileLoader, Saral.FileProcessor.IO.FileLoader>();
        services.AddScoped<Saral.FileProcessor.Core.Services.IValidationConfigurationService, Saral.FileProcessor.Core.Services.ValidationConfigurationService>();
        services.AddScoped<Saral.FileProcessor.Core.Services.IMultiFileValidator, Saral.FileProcessor.Core.Services.MultiFileValidator>();
        services.AddScoped<Saral.FileProcessor.Core.Abstractions.IDataQualityAnalyzer, Saral.FileProcessor.Core.Analysis.DataQualityAnalyzer>();

        // Add after existing service registrations
        services.AddScoped<ILoanDetailsRepository, LoanDetailsRepository>();
        services.AddScoped<ICashFlowCalculationService, CashFlowCalculationService>();
        services.AddScoped<ICashFlowConfigurationValidator, CashFlowConfigurationValidator>();

        return services;
    }

    private static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        string? redisConnectionString = configuration.GetConnectionString("Redis");

        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            // Register Redis connection as singleton
            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(redisConnectionString));
        }

        // Register PD Progress Publisher
        services.AddScoped<IPDProgressPublisher, PDProgressPublisher>();

        // Register LGD Progress Publisher
        services.AddScoped<ILgdProgressPublisher, LgdProgressPublisher>();

        // Register Cash Flow Discounting Service
        services.AddScoped<ICashFlowDiscountingService, CashFlowDiscountingService>();
        services.AddScoped<ICashFlowOrchestrationService, CashFlowOrchestrationService>();


        return services;
    }


    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<ApplicationDbContext>(
            options => options
                .UseNpgsql(connectionString, npgsqlOptions =>
                    npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Default))
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<DatabaseSeeder>();

        // Register Saral.FileProcessor DbContext
        services.AddDbContext<Saral.FileProcessor.Data.Context.FileProcessorDbContext>(options =>
            options.UseNpgsql(connectionString,
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable(
                    HistoryRepository.DefaultTableName, "fileprocessor")));

        return services;
    }

    private static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("Database")!);

        return services;
    }

    private static IServiceCollection AddAuthenticationInternal(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.RequireHttpsMetadata = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!)),
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenProvider, TokenProvider>();


        return services;
    }

    private static IServiceCollection AddAuthorizationInternal(this IServiceCollection services)
    {
        services.AddAuthorization();

        services.AddScoped<PermissionProvider>();

        services.AddScoped<IPermissionCacheService, PermissionCacheService>();

        services.AddTransient<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddTransient<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();

        return services;
    }
}
