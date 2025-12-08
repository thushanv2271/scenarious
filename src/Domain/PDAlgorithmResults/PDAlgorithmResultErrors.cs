using SharedKernel;

namespace Domain.PDAlgorithmResults;

/// <summary>
/// Error definitions for PD Algorithm Result operations
/// </summary>
public static class PDAlgorithmResultErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "PDAlgorithmResult.NotFound",
        $"PD Algorithm Result with ID '{id}' was not found");

    public static Error NoResultsFound => Error.NotFound(
        "PDAlgorithmResult.NoResultsFound",
        "No PD Algorithm Results found in the system");

    public static Error InvalidProductCategory(string productCategory) => Error.Validation(
        "PDAlgorithmResult.InvalidProductCategory",
        $"Product category '{productCategory}' was not found");

    public static Error InvalidSegment(string segment) => Error.Validation(
        "PDAlgorithmResult.InvalidSegment",
        $"Segment '{segment}' was not found");

    public static Error InvalidMethodology(string methodology) => Error.Validation(
        "PDAlgorithmResult.InvalidMethodology",
        $"Invalid methodology '{methodology}'. Valid values are: method1, method2, method3");

    public static Error UpdateFailed(string message) => Error.Failure(
        "PDAlgorithmResult.UpdateFailed",
        $"Failed to update PD Algorithm Result: {message}");
}
