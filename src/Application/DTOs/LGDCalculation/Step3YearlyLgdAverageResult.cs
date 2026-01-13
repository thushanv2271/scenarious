namespace Application.DTOs.LGDCalculation;

/// <summary>
/// Result of Step 3 LGD calculation containing yearly LGD averages and changes extracted from Step 2 results
/// </summary>
public sealed record Step3YearlyLgdAverageResult
{
    /// <summary>
    /// Gets the minimum number of years from NPL Date to Closure Date across all facilities
    /// </summary>
    public decimal MinYearsFromNplToClosureDate { get; init; }

    /// <summary>
    /// Gets the maximum number of years from NPL Date to Closure Date across all facilities
    /// </summary>
    public decimal MaxYearsFromNplToClosureDate { get; init; }

    /// <summary>
    /// Timestamp when the calculation was performed
    /// </summary>
    public DateTime CalculationTimestamp { get; init; }

    /// <summary>
    /// Gets the per-year results containing per-classification averages by years-from-NPL-to-closure
    /// </summary>
    public List<YearLgdAverageResult> YearResults { get; init; } = new();

    /// <summary>
    /// Gets the overall average LGD per classification per years-from-NPL-to-closure across all years.
    /// Returns null when no valid data found for that classification/years-from-NPL combination.
    /// </summary>
    public List<ClassificationOverallAverage> OverallAverageLgdByClassificationAndYears { get; init; } = new();
}

/// <summary>
/// Represents the yearly LGD average results for a specific year
/// </summary>
public sealed record YearLgdAverageResult
{
    /// <summary>
    /// The year (e.g. 2022)
    /// </summary>
    public int Year { get; init; }

    /// <summary>
    /// The per-LGD-classification results for this year
    /// </summary>
    public List<ClassificationLgdAverageResult> Classifications { get; init; } = new();
}

/// <summary>
/// Per-classification LGD averages grouped by yearsFromNplToClosureDate
/// </summary>
public sealed record ClassificationLgdAverageResult
{
    /// <summary>
    /// The LGD classification / segment name
    /// </summary>
    public string LgdClassification { get; init; } = string.Empty;

    /// <summary>
    /// Map of yearsFromNplToClosureDate => average LGD for that bucket.
    /// Returns null when no facilities found for that years-from-NPL-to-closure combination.
    /// </summary>
    public Dictionary<int, decimal?> AverageLgdByYearsFromNplToClosureDate { get; init; } = new();
}

/// <summary>
/// Request payload for Step 3 Yearly LGD Average calculation containing Step 2 hierarchical result
/// </summary>
public sealed record Step3YearlyLgdAverageRequest
{
    /// <summary>
    /// Gets the Step 2 hierarchical calculation result to process
    /// </summary>
    public HierarchicalStep2LgdCalculationResult Data { get; init; } = new();
}

/// <summary>
/// Represents overall average LGD data for a specific classification
/// </summary>
public sealed record ClassificationOverallAverage
{
    /// <summary>
    /// The LGD classification / segment name (e.g., "RETAIL", "SME", "CORPORATE")
    /// </summary>
    public string Classification { get; init; } = string.Empty;

    /// <summary>
    /// List of LGD averages and deltas by years from NPL to closure
    /// </summary>
    public List<YearsFromNplLgdData> YearsFromNplData { get; init; } = new();
}

/// <summary>
/// Represents LGD data for a specific number of years from NPL to closure
/// </summary>
public sealed record YearsFromNplLgdData
{
    /// <summary>
    /// Number of years from NPL to closure date (0, 1, 2, 3, etc.)
    /// </summary>
    public int YearsFromNplToClosureDate { get; init; }

    /// <summary>
    /// Average LGD percentage across all financial years for this classification and years-from-NPL combination
    /// Returns 0 if no data available (representing no loss/complete write-off scenario)
    /// </summary>
    public decimal AvgLgd { get; init; }

    /// <summary>
    /// Delta LGD percentage compared to previous years-from-NPL period
    /// Calculated as: Current AvgLgd - Previous AvgLgd
    /// Null if no previous data available
    /// </summary>
    public decimal? DeltaLgd { get; init; }
}