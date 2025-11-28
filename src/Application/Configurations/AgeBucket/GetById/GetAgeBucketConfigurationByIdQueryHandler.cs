using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Configurations.AgeBucket.Get;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Configurations.AgeBucket.GetById;

internal sealed class GetAgeBucketConfigurationByIdQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<GetAgeBucketConfigurationByIdQuery, AgeBucketConfigurationDto>
{
    public async Task<Result<AgeBucketConfigurationDto>> Handle(
        GetAgeBucketConfigurationByIdQuery request,
        CancellationToken cancellationToken)
    {
        AgeBucketConfigurationDto? configuration = await context.AgeBucketConfigurations
            .Where(config => config.Id == request.Id)
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
                config.ModifiedOnUtc))
            .FirstOrDefaultAsync(cancellationToken);

        if (configuration is null)
        {
            return Result.Failure<AgeBucketConfigurationDto>(
                new Error("AgeBucketConfiguration.NotFound", $"Age bucket configuration with ID '{request.Id}' was not found.", ErrorType.NotFound));
        }

        return Result.Success(configuration);
    }

    private static string MapStageToString(Domain.Stages.Stage stage) => stage switch
    {
        Domain.Stages.Stage.One => "stage1",
        Domain.Stages.Stage.Two => "stage2",
        Domain.Stages.Stage.Three => "stage3",
        _ => throw new ArgumentOutOfRangeException(nameof(stage))
    };
}