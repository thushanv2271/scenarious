// src/Domain/EclAnalysis/EclAnalysisErrors.cs
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

    // Invalid threshold type
    public static Error InvalidThresholdType => Error.Problem(
        "EclAnalysis.InvalidThresholdType",
        "Invalid threshold type specified. Must be CustomAbsolute, TopNCustomers, or CumulativePercentage.");

    // Invalid Top N count
    public static Error InvalidTopNCount => Error.Problem(
        "EclAnalysis.InvalidTopNCount",
        "Top N count must be greater than zero.");

    // Invalid cumulative percentage
    public static Error InvalidCumulativePercentage => Error.Problem(
        "EclAnalysis.InvalidCumulativePercentage",
        "Cumulative percentage must be between 0 and 100.");

    // Top N count exceeds available customers
    public static Error TopNExceedsCustomers(int requested, int available) => Error.Problem(
        "EclAnalysis.TopNExceedsCustomers",
        $"Requested top {requested} customers but only {available} customers are available.");

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
