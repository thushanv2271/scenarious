using Domain.Stages;

namespace Domain.Configurations;

public sealed record DatePassedDueBasedConfiguration(int RangeStart, int RangeEnd, string BucketLabel, Stage Stage);
