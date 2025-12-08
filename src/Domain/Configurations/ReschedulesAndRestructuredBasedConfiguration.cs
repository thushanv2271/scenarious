using Domain.Stages;

namespace Domain.Configurations;

public sealed record ReschedulesAndRestructuredBasedConfiguration(
    string? Id,
    string Reschedule,
    string Restructure,
    string BucketLabel,
    string StageMapping,
    bool HasBeenTouched,
    string? BucketLabelError,
    string? RestructureError,
    string? RescheduleError);
