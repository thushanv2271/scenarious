using Application.Abstractions.Messaging;
using Application.Configurations.AgeBucket.Get;

namespace Application.Configurations.AgeBucket.GetById;

/// <summary>
/// Query to get a specific age bucket configuration by ID
/// </summary>
public sealed record GetAgeBucketConfigurationByIdQuery(Guid Id) : IQuery<AgeBucketConfigurationDto>;