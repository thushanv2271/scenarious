namespace Web.Api.Endpoints.Collaterals;

/// <summary>
/// Request to create multiple collaterals.
/// </summary>
public sealed record CreateCollateralRequest(
    string[] Names
);