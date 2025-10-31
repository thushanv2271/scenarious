namespace Application.EfaConfigs.GetAll;

/// <summary>
/// Represents the data returned when retrieving an EFA configuration.
/// Contains details such as the configuration ID, year, rate, and update information.
/// </summary>
public sealed record GetAllEfaConfigurationResponse(
    Guid Id,
    int Year,
    decimal EfaRate,
    DateTime UpdatedAt,
    Guid UpdatedBy
);
