namespace Application.EfaConfigs.Edit;

/// <summary>
/// Response returned after successfully editing an EFA configuration.
/// Contains a summary of the updated configuration and update metadata.
/// </summary>
public sealed record EditEfaConfigurationResponse(
    Guid Id,
    int Year,
    decimal EfaRate,
    DateTime UpdatedAt,
    Guid UpdatedBy
);
