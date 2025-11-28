using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Configurations.AgeBucket.Get;

internal sealed class GetAgeBucketConfigurationsQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<GetAgeBucketConfigurationsQuery, PaginatedResult<AgeBucketConfigurationDto>>
{
    public async Task<Result<PaginatedResult<AgeBucketConfigurationDto>>> Handle(
        GetAgeBucketConfigurationsQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<AgeBucketConfigurationDto> query = context.AgeBucketConfigurations
            .Select(config => new AgeBucketConfigurationDto(
                config.Id,
                new AgeBucketConfigurationDataDto(
                    config.ConfigurationData.DatePassedDueBasedConfigurations
                        .Select(c => new DatePassedDueBasedConfigurationDto(
                            c.RangeStart,
                            c.RangeEnd,
                            c.BucketLabel,
                            MapStageToString(c.Stage)))
                        .ToList(),
                    config.ConfigurationData.ReschedulesAndRestructuredBasedConfigurations
                        .Select(c => new ReschedulesAndRestructuredBasedConfigurationDto(
                            c.Restructure,
                            c.Reschedule,
                            c.BucketLabel,
                            MapStageToString(c.Stage)))
                        .ToList(),
                    config.ConfigurationData.IndustryBasedConfigurations
                        .Select(c => new IndustryBasedConfigurationDto(
                            c.ProductCategoryId,
                            c.SegmentId,
                            c.IndustryId,
                            c.BucketLabel,
                            MapStageToString(c.Stage)))
                        .ToList()),
                config.CreatedOnUtc,
                config.ModifiedOnUtc));

        int totalCount = await context.AgeBucketConfigurations.CountAsync(cancellationToken);

        List<AgeBucketConfigurationDto> configurations = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var paginatedResult = new PaginatedResult<AgeBucketConfigurationDto>(
            configurations,
            totalCount);

        return Result.Success(paginatedResult);
    }

    private static string MapStageToString(Domain.Stages.Stage stage) => stage switch
    {
        Domain.Stages.Stage.One => "stage1",
        Domain.Stages.Stage.Two => "stage2",
        Domain.Stages.Stage.Three => "stage3",
        _ => throw new ArgumentOutOfRangeException(nameof(stage))
    };
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
    int RangeStart,
    int RangeEnd,
    string BucketLabel,
    string Stage);

public sealed record ReschedulesAndRestructuredBasedConfigurationDto(
    int Restructure,
    bool Reschedule,
    string BucketLabel,
    string Stage);

public sealed record IndustryBasedConfigurationDto(
    Guid ProductCategoryId,
    Guid SegmentId,
    Guid IndustryId,
    string BucketLabel,
    string Stage);