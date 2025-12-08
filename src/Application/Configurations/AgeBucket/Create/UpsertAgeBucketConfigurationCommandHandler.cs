using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Configurations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Configurations.AgeBucket.Create;

internal sealed class UpsertAgeBucketConfigurationCommandHandler(
    IApplicationDbContext context)
    : ICommandHandler<UpsertAgeBucketConfigurationCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        UpsertAgeBucketConfigurationCommand command,
        CancellationToken cancellationToken)
    {
        var configurationData = new AgeBucketConfigurationData
        {
            DatePassedDueBasedConfigurations = command.DatePassedDueBasedConfigurations
                .Select(dto => new DatePassedDueBasedConfiguration(
                    dto.Id,
                    dto.RangeStart,
                    dto.RangeEnd,
                    dto.BucketLabel,
                    dto.StageMapping,
                    dto.RangeEndError,
                    dto.HasBeenTouched))
                .ToList(),
            ReschedulesAndRestructuredBasedConfigurations = command.ReschedulesAndRestructuredBasedConfigurations
                .Select(dto => new ReschedulesAndRestructuredBasedConfiguration(
                    dto.Id,
                    dto.Reschedule,
                    dto.Restructure,
                    dto.BucketLabel,
                    dto.StageMapping,
                    dto.HasBeenTouched,
                    dto.BucketLabelError,
                    dto.RestructureError,
                    dto.RescheduleError))
                .ToList(),
            IndustryBasedConfigurations = command.IndustryBasedConfigurations
                .Select(dto => new IndustryBasedConfiguration(
                    dto.Id,
                    dto.ProductCategory,
                    dto.Segment,
                    dto.Industry,
                    dto.BucketLabel,
                    dto.StageMapping,
                    dto.HasBeenTouched,
                    dto.ProductCategoryError,
                    dto.SegmentError,
                    dto.IndustryError,
                    dto.BucketLabelError))
                .ToList()
        };

        // Check if configuration already exists (assuming single configuration model)
        AgeBucketConfiguration? existingConfig = await context.AgeBucketConfigurations
            .FirstOrDefaultAsync(cancellationToken);

        if (existingConfig is not null)
        {
            // Update existing configuration
            existingConfig.ConfigurationData = configurationData;
            existingConfig.ModifiedOnUtc = DateTime.UtcNow;
            
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(existingConfig.Id);
        }
        
        // Create new configuration
        var newConfiguration = new AgeBucketConfiguration
        {
            Id = Guid.NewGuid(),
            ConfigurationData = configurationData,
            CreatedOnUtc = DateTime.UtcNow
        };

        context.AgeBucketConfigurations.Add(newConfiguration);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(newConfiguration.Id);
    }
}
