using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Configurations.AgeBucket.Get;

/// <summary>
/// Query to get the single age bucket configuration (no pagination)
/// </summary>
public sealed record GetAgeBucketConfigurationQuery() : IQuery<AgeBucketConfigurationDto>;
