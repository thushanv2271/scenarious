namespace Web.Api.Endpoints.PdCalculation.ExecuteStep3HistoricalPd;

/// <summary>
/// Response for Step 3 - Historical PD Generation
/// Contains structured average PD tables organized by product category and segment
/// </summary>
/// <param name="Success">Indicates if the operation was successful</param>
/// <param name="Message">Descriptive message about the operation</param>
/// <param name="Timestamp">Timestamp when the operation completed</param>
/// <param name="AveragePDTables">Dictionary of product categories containing segment average PD tables</param>
public sealed record Response(
    bool Success,
    string Message,
    DateTime Timestamp,
    Dictionary<string, Dictionary<string, SegmentTableResponse>> AveragePDTables
);

/// <summary>
/// Represents a segment's average PD table with historical and interpolated PDs
/// </summary>
public sealed record SegmentTableResponse(
    List<string> ColumnHeaders,
    List<BucketRowResponse> Rows,
    int BucketCount,
    int HighestMaturity
);

/// <summary>
/// Represents a row in the average PD table for a specific bucket
/// </summary>
public sealed record BucketRowResponse(
    string AgeBucket,
    decimal HistoricalPD,
    decimal InterpolatedPD
);
