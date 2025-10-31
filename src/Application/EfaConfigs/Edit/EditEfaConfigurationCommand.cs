using Application.Abstractions.Messaging;

namespace Application.EfaConfigs.Edit;

/// <summary>
/// Command to edit an existing EFA configuration.
/// </summary>
public sealed record EditEfaConfigurationCommand(
    Guid Id,
    int Year,
    decimal EfaRate,
    Guid UpdatedBy
) : ICommand<EditEfaConfigurationResponse>;
