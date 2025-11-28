using Domain.Stages;

namespace Domain.Configurations;

public sealed record ReschedulesAndRestructuredBasedConfiguration(int Restructure, bool Reschedule, string BucketLabel, Stage Stage);
