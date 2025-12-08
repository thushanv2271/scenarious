using Domain.Stages;

namespace Domain.Configurations;

public sealed record IndustryBasedConfiguration(
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
