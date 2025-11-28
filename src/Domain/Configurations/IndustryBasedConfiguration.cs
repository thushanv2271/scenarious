using Domain.Stages;

namespace Domain.Configurations;

public sealed record IndustryBasedConfiguration(Guid ProductCategoryId, Guid SegmentId, Guid IndustryId, string BucketLabel, Stage Stage);
