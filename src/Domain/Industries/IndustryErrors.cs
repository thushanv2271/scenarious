using SharedKernel;

namespace Domain.Industries;

/// <summary>
/// Contains error definitions for Industry domain operations.
/// </summary>
public static class IndustryErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Industry.NotFound",
        "The industry with the specified identifier was not found");

    public static readonly Error NameEmpty = Error.Problem(
        "Industry.NameEmpty",
        "Industry name cannot be empty");

    public static readonly Error NameTooLong = Error.Problem(
        "Industry.NameTooLong",
        "Industry name cannot exceed 255 characters");

    public static readonly Error DuplicateName = Error.Problem(
        "Industry.DuplicateName",
        "An industry with the same name already exists");

    public static readonly Error EmptyArray = Error.Problem(
        "Industry.EmptyArray",
        "Industry names array cannot be empty");
}