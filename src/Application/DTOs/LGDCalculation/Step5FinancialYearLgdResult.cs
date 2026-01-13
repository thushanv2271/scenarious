namespace Application.DTOs.LGDCalculation;

/// <summary>
/// Result of Step 5 LGD calculation containing financial year-based LGD analysis from Step 2 hierarchical results
/// </summary>
public sealed record Step5FinancialYearLgdResult
{
    /// <summary>
    /// Gets the financial year-based LGD analysis results organized by financial year period
    /// </summary>
    public List<FinancialYearLgdSummary> FinancialYearSummaries { get; init; } = new();

    /// <summary>
    /// Gets the list of all available classifications across all financial years
    /// </summary>
    public List<string> AllAvailableClassifications { get; init; } = new();

    /// <summary>
    /// Gets the final LGD percentages by classification calculated from all financial years
    /// </summary>
    public Dictionary<string, decimal> FinalLgdPercentagesByClassification { get; init; } = new();

    /// <summary>
    /// Gets the timestamp when the calculation was performed
    /// </summary>
    public DateTime CalculationTimestamp { get; init; }
}

/// <summary>
/// Represents LGD summary for a specific financial year period
/// </summary>
public sealed record FinancialYearLgdSummary
{
    /// <summary>
    /// Gets the financial year end date
    /// </summary>
    public DateTime FinancialYearEndDate { get; init; }

    /// <summary>
    /// Gets the LGD classification summaries for this financial year
    /// </summary>
    public List<FinancialYearClassificationSummary> Classifications { get; init; } = new();
}

/// <summary>
/// Represents LGD summary for a specific classification within a financial year
/// </summary>
public sealed record FinancialYearClassificationSummary
{
    /// <summary>
    /// Gets the LGD classification name
    /// </summary>
    public string ClassificationName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the sum of Total Outstanding as at First NPL Date for this classification
    /// </summary>
    public decimal TotalOutstandingAsAtFirstNplDate { get; init; }

    /// <summary>
    /// Gets the sum of loss for this classification
    /// </summary>
    public decimal TotalLoss { get; init; }
}

/// <summary>
/// Request payload for Step 5 Financial Year LGD analysis containing Step 2 hierarchical result and financial year ends
/// </summary>
public sealed record Step5FinancialYearLgdRequest
{
    /// <summary>
    /// Gets the Step 2 hierarchical calculation result to process
    /// </summary>
    public HierarchicalStep2LgdCalculationResult Data { get; init; } = new();

    /// <summary>
    /// Gets the list of financial year end dates (e.g., "31-Dec-19", "31-Dec-20", "31-Dec-21", "31-Dec-22", "31-Dec-23")
    /// </summary>
    public List<string> FinancialYearEnds { get; init; } = new();
}