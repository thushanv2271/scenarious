using Application.Abstractions.Messaging;

namespace Application.Configurations.AgeBucket.Create;

/// <summary>
/// Command to create a new age bucket configuration
/// </summary>
public sealed class CreateAgeBucketConfigurationCommand : ICommand<Guid>
{
    public CreateAgeBucketConfigurationDataDto ConfigurationData { get; set; } = new();
}

public sealed class CreateAgeBucketConfigurationDataDto
{
    public ICollection<CreateDatePassedDueBasedConfigurationDto> DatePassedDueBasedConfigurations { get; set; } = [];
    public ICollection<CreateReschedulesAndRestructuredBasedConfigurationDto> ReschedulesAndRestructuredBasedConfigurations { get; set; } = [];
    public ICollection<CreateIndustryBasedConfigurationDto> IndustryBasedConfigurations { get; set; } = [];
}

public sealed record CreateDatePassedDueBasedConfigurationDto(
    int RangeStart,
    int RangeEnd,
    string BucketLabel,
    string Stage);

public sealed record CreateReschedulesAndRestructuredBasedConfigurationDto(
    int Restructure,
    bool Reschedule,
    string BucketLabel,
    string Stage);

public sealed record CreateIndustryBasedConfigurationDto(
    Guid ProductCategoryId,
    Guid SegmentId,
    Guid IndustryId,
    string BucketLabel,
    string Stage);