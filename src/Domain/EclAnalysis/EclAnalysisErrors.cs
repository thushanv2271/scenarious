using SharedKernel;

namespace Domain.EclAnalysis;

/// <summary>
/// Defines error codes and messages for ECL analysis operations
/// </summary>
public static class EclAnalysisErrors
{
    // Threshold value is invalid (zero or negative)
    public static Error InvalidThreshold => Error.Problem(
        "EclAnalysis.InvalidThreshold",
        "The individual significant threshold must be greater than zero.");

    // No loan records found for the given criteria
    public static Error NoDataFound => Error.Problem(
        "EclAnalysis.NoDataFound",
        "No loan data found for the specified criteria.");

    // User lacks branch assignment
    public static Error UserNotAssignedToBranch => Error.Problem(
        "EclAnalysis.UserNotAssignedToBranch",
        "User is not assigned to any branch. Please contact administrator.");

    // Generic processing error with custom message
    public static Error ProcessingError(string message) => Error.Problem(
        "EclAnalysis.ProcessingError",
        $"Error processing ECL threshold analysis: {message}");
}
