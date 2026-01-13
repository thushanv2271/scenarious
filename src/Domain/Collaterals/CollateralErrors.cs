using SharedKernel;

namespace Domain.Collaterals;

/// <summary>
/// Contains error definitions for Collateral domain operations.
/// </summary>
public static class CollateralErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Collateral.NotFound",
        "The collateral with the specified identifier was not found");

    public static readonly Error NameEmpty = Error.Problem(
        "Collateral.NameEmpty",
        "Collateral name cannot be empty");

    public static readonly Error NameTooLong = Error.Problem(
        "Collateral.NameTooLong",
        "Collateral name cannot exceed 255 characters");

    public static readonly Error DuplicateName = Error.Problem(
        "Collateral.DuplicateName",
        "A collateral with the same name already exists");

    public static readonly Error EmptyArray = Error.Problem(
        "Collateral.EmptyArray",
        "Collateral names array cannot be empty");
}