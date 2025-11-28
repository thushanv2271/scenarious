using Application.Abstractions.Messaging;
using Application.Configurations.AgeBucket.Create;

namespace Application.Configurations.AgeBucket.Update;

/// <summary>
/// Command to update an existing age bucket configuration
/// </summary>
public sealed class UpdateAgeBucketConfigurationCommand : ICommand<bool>
{
    public Guid Id { get; set; }
    public CreateAgeBucketConfigurationDataDto ConfigurationData { get; set; } = new();
}