using Application.Abstractions.Messaging;

namespace Application.Configurations.AgeBucket.Delete;

/// <summary>
/// Command to delete an age bucket configuration
/// </summary>
public sealed record DeleteAgeBucketConfigurationCommand(Guid Id) : ICommand<bool>;