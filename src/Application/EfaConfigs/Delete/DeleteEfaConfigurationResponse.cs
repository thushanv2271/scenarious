namespace Application.EfaConfigs.Delete;

/// <summary>
/// Response returned after successfully deleting an EFA configuration.
/// Contains a summary of the deleted configuration and deletion metadata.
/// </summary>

public sealed record DeleteEfaConfigurationResponse(
    Guid Id,
    int Year,
    decimal EfaRate,
    DateTime DeletedAt,
    Guid DeletedBy
);
