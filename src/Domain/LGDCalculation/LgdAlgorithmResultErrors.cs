using SharedKernel;

namespace Domain.LGDCalculation;

/// <summary>
/// Domain errors for LGD Algorithm Result operations
/// </summary>
public static class LgdAlgorithmResultErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "LgdAlgorithmResult.NotFound",
        $"LGD Algorithm Result with ID '{id}' was not found");

    public static readonly Error NoResultsFound = Error.NotFound(
        "LgdAlgorithmResult.NoResultsFound",
        "No LGD Algorithm Results found in the system");

    public static readonly Error InvalidData = Error.Validation(
        "LgdAlgorithmResult.InvalidData",
        "The LGD algorithm result data is invalid");

    public static readonly Error InvalidJsonFormat = Error.Validation(
        "LgdAlgorithmResult.InvalidJsonFormat",
        "The provided JSON data has an invalid format");

    public static Error InvalidProductCategory(string productCategory) => Error.Validation(
        "LgdAlgorithmResult.InvalidProductCategory",
        $"Product category '{productCategory}' was not found");

    public static Error InvalidSegment(string segment) => Error.Validation(
        "LgdAlgorithmResult.InvalidSegment",
        $"Segment '{segment}' was not found");

    public static Error UpdateFailed(string message) => Error.Failure(
        "LgdAlgorithmResult.UpdateFailed",
        $"Failed to update LGD Algorithm Result: {message}");
}