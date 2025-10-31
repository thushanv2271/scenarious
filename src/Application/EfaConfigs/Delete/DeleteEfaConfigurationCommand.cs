using Application.Abstractions.Messaging;

namespace Application.EfaConfigs.Delete;

/// <summary>
/// Command to delete an existing EFA configuration by its unique identifier.
/// </summary>

public sealed record DeleteEfaConfigurationCommand(
    Guid Id,
    Guid DeletedBy
) : ICommand<DeleteEfaConfigurationResponse>;
