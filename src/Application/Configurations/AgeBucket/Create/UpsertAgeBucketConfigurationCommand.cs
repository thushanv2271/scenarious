using Application.Abstractions.Messaging;

namespace Application.Configurations.AgeBucket.Create;

/// <summary>
/// Command to upsert an age bucket configuration (create or replace existing)
/// </summary>
public sealed class UpsertAgeBucketConfigurationCommand : ICommand<Guid>
{
    public ICollection<UpsertDatePassedDueBasedConfigurationDto> DatePassedDueBasedConfigurations { get; set; } = [];
    public ICollection<UpsertReschedulesAndRestructuredBasedConfigurationDto> ReschedulesAndRestructuredBasedConfigurations { get; set; } = [];
    public ICollection<UpsertIndustryBasedConfigurationDto> IndustryBasedConfigurations { get; set; } = [];
}

public sealed record UpsertDatePassedDueBasedConfigurationDto(
    string? Id,
    string RangeStart,
    string RangeEnd,
    string BucketLabel,
    string StageMapping,
    string? RangeEndError,
    bool HasBeenTouched);

public sealed record UpsertReschedulesAndRestructuredBasedConfigurationDto(
    string? Id,
    string Reschedule,
    string Restructure,
    string BucketLabel,
    string StageMapping,
    bool HasBeenTouched,
    string? BucketLabelError,
    string? RestructureError,
    string? RescheduleError);

public sealed record UpsertIndustryBasedConfigurationDto(
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