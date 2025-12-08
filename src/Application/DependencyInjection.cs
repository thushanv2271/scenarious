using Application.Abstractions.Behaviors;
using Application.Abstractions.Messaging;
using Application.Files.Services;
using Application.Services;
using FluentValidation;
using Saral.FileProcessor.Reports.Services;
using SharedKernel;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssembliesOf(typeof(DependencyInjection))
            .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(IDomainEventHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

        services.Decorate(typeof(ICommandHandler<,>), typeof(ValidationDecorator.CommandHandler<,>));
        services.Decorate(typeof(ICommandHandler<>), typeof(ValidationDecorator.CommandBaseHandler<>));

        services.Decorate(typeof(IQueryHandler<,>), typeof(LoggingDecorator.QueryHandler<,>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(LoggingDecorator.CommandHandler<,>));
        services.Decorate(typeof(ICommandHandler<>), typeof(LoggingDecorator.CommandBaseHandler<>));

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        // Register PDSetupConfigurationService
        services.AddScoped<IPDSetupConfigurationService, PDSetupConfigurationService>();
        // Register specific handlers
        services.AddScoped<ProductCategories.UploadProductCategoriesAndSegmentsHandler>();
        services.AddScoped<ProductCategories.SearchProductCategoriesAndSegmentsHandler>();

        // Register file movement service
        services.AddScoped<IFileMovementService, FileMovementService>();

        // Saral.FileProcessor Core services
        services.AddScoped<Saral.FileProcessor.Core.Services.IFileProcessingService, Saral.FileProcessor.Core.Services.FileProcessingService>();

        // Saral.FileProcessor Data services
        services.AddScoped<Saral.FileProcessor.Data.Services.IFileProcessingResultService, Saral.FileProcessor.Data.Services.FileProcessingResultService>();

        // Saral.FileProcessor Reports services
        services.AddScoped<Saral.FileProcessor.Reports.Services.IReportGenerationService, Saral.FileProcessor.Reports.Services.ReportGenerationService>();

        return services;
    }
}
