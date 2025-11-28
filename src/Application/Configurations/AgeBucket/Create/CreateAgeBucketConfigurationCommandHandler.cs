using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Configurations;
using Domain.Stages;
using SharedKernel;

namespace Application.Configurations.AgeBucket.Create;

internal sealed class CreateAgeBucketConfigurationCommandHandler(
    IApplicationDbContext context)
    : ICommandHandler<CreateAgeBucketConfigurationCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateAgeBucketConfigurationCommand command,
        CancellationToken cancellationToken)
    {
        var configurationData = new AgeBucketConfigurationData
        {
            DatePassedDueBasedConfigurations = command.ConfigurationData.DatePassedDueBasedConfigurations
                .Select(dto => new DatePassedDueBasedConfiguration(
                    dto.RangeStart,
                    dto.RangeEnd,
                    dto.BucketLabel,
                    ParseStage(dto.Stage)))
                .ToList(),
            ReschedulesAndRestructuredBasedConfigurations = command.ConfigurationData.ReschedulesAndRestructuredBasedConfigurations
                .Select(dto => new ReschedulesAndRestructuredBasedConfiguration(
                    dto.Restructure,
                    dto.Reschedule,
                    dto.BucketLabel,
                    ParseStage(dto.Stage)))
                .ToList(),
            IndustryBasedConfigurations = command.ConfigurationData.IndustryBasedConfigurations
                .Select(dto => new IndustryBasedConfiguration(
                    dto.ProductCategoryId,
                    dto.SegmentId,
                    dto.IndustryId,
                    dto.BucketLabel,
                    ParseStage(dto.Stage)))
                .ToList()
        };

        var ageBucketConfiguration = new AgeBucketConfiguration
        {
            Id = Guid.NewGuid(),
            ConfigurationData = configurationData,
            CreatedOnUtc = DateTime.UtcNow
        };

        context.AgeBucketConfigurations.Add(ageBucketConfiguration);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(ageBucketConfiguration.Id);
    }

    private static Stage ParseStage(string stage) => stage switch
    {
        "stage1" or "Stage1" or "STAGE1" => Stage.One,
        "stage2" or "Stage2" or "STAGE2" => Stage.Two,
        "stage3" or "Stage3" or "STAGE3" => Stage.Three,
        _ => throw new ArgumentException($"Invalid stage value: {stage}")
    };
}