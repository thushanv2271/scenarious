using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Configurations.AgeBucket.Get;

/// <summary>
/// Query to get age bucket configurations with pagination
/// </summary>
public sealed record GetAgeBucketConfigurationsQuery(
    int Page = 1,
    int PageSize = 10) : IQuery<PaginatedResult<AgeBucketConfigurationDto>>;