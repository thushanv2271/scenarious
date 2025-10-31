using Application.Abstractions.Messaging;

namespace Application.EfaConfigs.Create;

/// <summary>
/// Command to create one or more EFA configurations.
/// Wraps a collection of configuration items along with the user who performed the update.
/// </summary>
public sealed record CreateEfaConfigurationCommand(
    List<EfaConfigurationItem> Items,
    Guid UpdatedBy
) : ICommand<CreateEfaConfigurationResponse>;

/// <summary>
/// Represents an individual EFA configuration item to be created.
/// Contains the configuration year and its associated EFA rate.
/// </summary>
public sealed record EfaConfigurationItem(
    int Year,
    decimal EfaRate
);
