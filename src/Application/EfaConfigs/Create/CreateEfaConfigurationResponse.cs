namespace Application.EfaConfigs.Create;

/// <summary>
/// Contains lists of configurations that were created and updated.
/// </summary>
public sealed record CreateEfaConfigurationResponse(
    List<EfaConfigurationSummary> Created,
    List<EfaConfigurationSummary> Updated
);

/// <summary>
/// Represents a summary of an EFA configuration, either newly created or updated.
/// </summary>
public sealed record EfaConfigurationSummary(
    Guid Id,
    int Year,
    decimal EfaRate,
    DateTime UpdatedAt,
    Guid UpdatedBy
);
