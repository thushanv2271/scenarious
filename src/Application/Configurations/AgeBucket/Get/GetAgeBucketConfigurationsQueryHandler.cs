using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Configurations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Configurations.AgeBucket.Get;

internal sealed class GetAgeBucketConfigurationsQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<GetAgeBucketConfigurationQuery, AgeBucketConfigurationDto>
{
    public async Task<Result<AgeBucketConfigurationDto>> Handle(
        GetAgeBucketConfigurationQuery request,
        CancellationToken cancellationToken)
    {
        // Assumes there is always at most one configuration record in DB
        AgeBucketConfiguration? config = await context.AgeBucketConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (config is null)
        {
            return Result.Failure<AgeBucketConfigurationDto>(
                Error.NotFound("AgeBucketConfiguration.NotFound", "No age bucket configuration found"));
        }

        var dto = new AgeBucketConfigurationDto(
            config.Id,
            new AgeBucketConfigurationDataDto(
                config.ConfigurationData.DatePassedDueBasedConfigurations?
                    .Select(c => new DatePassedDueBasedConfigurationDto(
                        c.Id,
                        c.RangeStart,
                        c.RangeEnd,
                        c.BucketLabel,
                        c.StageMapping,
                        c.RangeEndError,
                        c.HasBeenTouched))
                    .ToList() ?? [],
                config.ConfigurationData.ReschedulesAndRestructuredBasedConfigurations?
                    .Select(c => new ReschedulesAndRestructuredBasedConfigurationDto(
                        c.Id,
                        c.Reschedule,
                        c.Restructure,
                        c.BucketLabel,
                        c.StageMapping,
                        c.HasBeenTouched,
                        c.BucketLabelError,
                        c.RestructureError,
                        c.RescheduleError))
                    .ToList() ?? [],
                config.ConfigurationData.IndustryBasedConfigurations?
                    .Select(c => new IndustryBasedConfigurationDto(
                        c.Id,
                        c.ProductCategory,
                        c.Segment,
                        c.Industry,
                        c.BucketLabel,
                        c.StageMapping,
                        c.HasBeenTouched,
                        c.ProductCategoryError,
                        c.SegmentError,
                        c.IndustryError,
                        c.BucketLabelError))
                    .ToList() ?? []),
            config.CreatedOnUtc,
            config.ModifiedOnUtc);

        return Result.Success(dto);
    }
}

public sealed record AgeBucketConfigurationDto(
    Guid Id,
    AgeBucketConfigurationDataDto ConfigurationData,
    DateTime CreatedOnUtc,
    DateTime? ModifiedOnUtc);

public sealed record AgeBucketConfigurationDataDto(
    ICollection<DatePassedDueBasedConfigurationDto> DatePassedDueBasedConfigurations,
    ICollection<ReschedulesAndRestructuredBasedConfigurationDto> ReschedulesAndRestructuredBasedConfigurations,
    ICollection<IndustryBasedConfigurationDto> IndustryBasedConfigurations);

public sealed record DatePassedDueBasedConfigurationDto(
    string? Id,
    string RangeStart,
    string RangeEnd,
    string BucketLabel,
    string StageMapping,
    string? RangeEndError,
    bool HasBeenTouched);

public sealed record ReschedulesAndRestructuredBasedConfigurationDto(
    string? Id,
    string Reschedule,
    string Restructure,
    string BucketLabel,
    string StageMapping,
    bool HasBeenTouched,
    string? BucketLabelError,
    string? RestructureError,
    string? RescheduleError);

public sealed record IndustryBasedConfigurationDto(
    string? Id,
    string ProductCategory,
    string Segment,
    string Industry,
    string BucketLabel,
    string StageMapping,
    bool HasBeenTouched,
    string? ProductCategoryError = null,
    string? SegmentError = null,
    string? IndustryError = null,
    string? BucketLabelError = null);
