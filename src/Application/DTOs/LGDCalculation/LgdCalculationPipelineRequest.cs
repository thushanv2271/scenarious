namespace Application.DTOs.LGDCalculation;

/// <summary>
/// Request payload for LGD Calculation Pipeline execution
/// </summary>
public sealed record LgdCalculationPipelineRequest
{
    /// <summary>
    /// Optional session ID for progress tracking. If provided, will use existing progress records.
    /// If not provided, will create new progress records.
    /// </summary>
    public Guid? SessionId { get; init; }

    /// <summary>
    /// Optional VC points by classification. If provided, Step 4 will be skipped and these values will be used.
    /// If not provided or empty, Step 4 will be executed to determine VC points automatically.
    /// </summary>
    public Dictionary<string, decimal>? VcPointsByClassification { get; init; }

    /// <summary>
    /// Required list of financial year end dates for Step 5 analysis (e.g., "31-Dec-19", "31-Dec-20", "31-Dec-21")
    /// </summary>
    public List<string> FinancialYearEnds { get; init; } = new();
}