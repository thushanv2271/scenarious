using Application.Models;

namespace Web.Api.Endpoints.PdCalculation.ExecuteStep2MatrixGeneration;

/// <summary>
/// Response for Step 2 - Migration Matrix Generation
/// </summary>
/// <param name="Success">Indicates if the operation was successful</param>
/// <param name="Message">Descriptive message about the operation</param>
/// <param name="Timestamp">Timestamp when the operation completed</param>
/// <param name="Matrices">Collection of period migration matrices with segment breakdown</param>
public sealed record Response(
    bool Success,
    string Message,
    DateTime Timestamp,
    IReadOnlyList<MatrixResponse> Matrices
);

/// <summary>
/// Represents a period migration matrix in the response
/// </summary>
public sealed record MatrixResponse(
    string PeriodNMinus1,
    string PeriodN,
    List<ProductCategoryResponse> ProductCategories
);

/// <summary>
/// Represents a product category with its segments
/// </summary>
public sealed record ProductCategoryResponse(
    string ProductCategoryName,
    List<SegmentResponse> Segments
);

/// <summary>
/// Represents a segment with its migration data
/// </summary>
public sealed record SegmentResponse(
    string SegmentName,
    string ProductCategory,
    string ComparisonPeriod,
    List<string> Buckets,
    int BucketCount,
    bool HasPercentageData,
    CountsResponse Counts,
    PercentagesResponse? Percentages
);

/// <summary>
/// Represents count data for a segment
/// </summary>
public sealed record CountsResponse(
    List<List<int>> Matrix,
    List<int> ExitCounts,
    List<int> RowTotals
);

/// <summary>
/// Represents percentage data for a segment
/// </summary>
public sealed record PercentagesResponse(
    List<List<double?>> Matrix,
    List<double?> ExitPercentages,
    List<double> GrandTotal
);
