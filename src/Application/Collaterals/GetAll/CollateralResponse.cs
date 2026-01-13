namespace Application.Collaterals.GetAll;

/// <summary>
/// Response containing collateral information.
/// </summary>
public sealed record CollateralResponse(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    DateTime UpdatedAt
);