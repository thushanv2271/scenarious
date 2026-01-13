namespace Application.Collaterals.Create;

/// <summary>
/// Response for the create collateral command.
/// </summary>
public sealed record CreateCollateralResponse(
    bool Success,
    int TotalProcessed,
    int CreatedCount,
    int SkippedCount,
    IReadOnlyList<CreatedCollateral> CreatedCollaterals,
    IReadOnlyList<string> SkippedNames
);

/// <summary>
/// Represents a successfully created collateral.
/// </summary>
public sealed record CreatedCollateral(
    Guid Id,
    string Name,
    DateTime CreatedAt
);