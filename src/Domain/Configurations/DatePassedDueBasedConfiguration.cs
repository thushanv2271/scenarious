using Domain.Stages;

namespace Domain.Configurations;

public sealed record DatePassedDueBasedConfiguration(
    string? Id,
    string RangeStart,
    string RangeEnd,
    string BucketLabel,
    string StageMapping,
    string? RangeEndError,
    bool HasBeenTouched);
