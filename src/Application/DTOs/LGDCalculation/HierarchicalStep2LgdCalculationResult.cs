namespace Application.DTOs.LGDCalculation;

/// <summary>
/// Hierarchical result of Step 2 LGD calculation with year, LGD classification (segment), and facility structure
/// </summary>
public sealed record HierarchicalStep2LgdCalculationResult
{
    /// <summary>
    /// Gets the hierarchical data organized by year, then by LGD classification (segment), then by facility
    /// </summary>
    public List<YearLgdClassification> YearClassifications { get; init; } = new();

    /// <summary>
    /// Gets the total number of unique facilities processed across all years and segments
    /// </summary>
    public int TotalFacilities { get; init; }

    /// <summary>
    /// Gets the grand total of all discounted cashflows across all years, segments, and facilities
    /// </summary>
    public decimal GrandTotalDiscountedCashflows { get; init; }

    /// <summary>
    /// Gets the total number of LGD records processed
    /// </summary>
    public int TotalRecordsProcessed { get; init; }

    /// <summary>
    /// Gets the timestamp when the calculation was performed
    /// </summary>
    public DateTime CalculationTimestamp { get; init; }

    /// <summary>
    /// Gets the total portfolio loss across all years, segments, and facilities
    /// </summary>
    public decimal TotalPortfolioLoss { get; init; }

    /// <summary>
    /// Gets the average LGD (Loss Given Default) across all years, segments, and facilities
    /// </summary>
    public decimal AverageLgd { get; init; }

    /// <summary>
    /// Gets the average recovery rate across all years, segments, and facilities
    /// </summary>
    public decimal AverageRecoveryRate { get; init; }

    /// <summary>
    /// Gets the total maximum outstanding amount across all years, segments, and facilities
    /// </summary>
    public decimal TotalMaxOutstanding { get; init; }

    /// <summary>
    /// Gets the average number of years from NPL Date to Closure Date across all facilities
    /// </summary>
    public decimal AverageYearsFromNplToClosureDate { get; init; }

    /// <summary>
    /// Gets the minimum number of years from NPL Date to Closure Date across all facilities
    /// </summary>
    public decimal MinYearsFromNplToClosureDate { get; init; }

    /// <summary>
    /// Gets the maximum number of years from NPL Date to Closure Date across all facilities
    /// </summary>
    public decimal MaxYearsFromNplToClosureDate { get; init; }

    /// <summary>
    /// Gets the number of facilities with zero years from NPL to closure
    /// </summary>
    public int FacilitiesWithZeroYearsToClosureFromNpl { get; init; }

    /// <summary>
    /// Gets the summary statistics for each year
    /// </summary>
    public List<YearSummaryStatistics> YearSummaries { get; init; } = new();
}

/// <summary>
/// Represents LGD classifications (segments) for a specific year
/// </summary>
public sealed record YearLgdClassification
{
    /// <summary>
    /// Gets the year
    /// </summary>
    public int Year { get; init; }

    /// <summary>
    /// Gets the LGD classifications (segments) and their facility summaries
    /// </summary>
    public List<SegmentLgdClassification> LgdClassifications { get; init; } = new();

    /// <summary>
    /// Gets the total facilities for this year
    /// </summary>
    public int TotalFacilities { get; init; }

    /// <summary>
    /// Gets the total discounted cashflows for this year
    /// </summary>
    public decimal TotalDiscountedCashflows { get; init; }

    /// <summary>
    /// Gets the total portfolio loss for this year
    /// </summary>
    public decimal TotalPortfolioLoss { get; init; }

    /// <summary>
    /// Gets the average LGD for this year
    /// </summary>
    public decimal AverageLgd { get; init; }

    /// <summary>
    /// Gets the total records processed for this year
    /// </summary>
    public int TotalRecordsProcessed { get; init; }
}

/// <summary>
/// Represents facility summaries for a specific LGD classification (segment) within a year
/// </summary>
public sealed record SegmentLgdClassification
{
    /// <summary>
    /// Gets the LGD classification (segment name)
    /// </summary>
    public string LgdClassification { get; init; } = string.Empty;

    /// <summary>
    /// Gets the facility summaries for this segment
    /// </summary>
    public List<FacilityDiscountedCashflowSummary> FacilitySummaries { get; init; } = new();

    /// <summary>
    /// Gets the total facilities for this segment
    /// </summary>
    public int TotalFacilities { get; init; }

    /// <summary>
    /// Gets the total discounted cashflows for this segment
    /// </summary>
    public decimal TotalDiscountedCashflows { get; init; }

    /// <summary>
    /// Gets the total portfolio loss for this segment
    /// </summary>
    public decimal TotalPortfolioLoss { get; init; }

    /// <summary>
    /// Gets the average LGD for this segment
    /// </summary>
    public decimal AverageLgd { get; init; }

    /// <summary>
    /// Gets the average recovery rate for this segment
    /// </summary>
    public decimal AverageRecoveryRate { get; init; }

    /// <summary>
    /// Gets the total maximum outstanding for this segment
    /// </summary>
    public decimal TotalMaxOutstanding { get; init; }

    /// <summary>
    /// Gets the total records processed for this segment
    /// </summary>
    public int TotalRecordsProcessed { get; init; }
}

/// <summary>
/// Summary statistics for a specific year
/// </summary>
public sealed record YearSummaryStatistics
{
    /// <summary>
    /// Gets the year
    /// </summary>
    public int Year { get; init; }

    /// <summary>
    /// Gets the number of unique segments in this year
    /// </summary>
    public int UniqueSegments { get; init; }

    /// <summary>
    /// Gets the segment names in this year
    /// </summary>
    public List<string> SegmentNames { get; init; } = new();

    /// <summary>
    /// Gets the total facilities for this year
    /// </summary>
    public int TotalFacilities { get; init; }

    /// <summary>
    /// Gets the total discounted cashflows for this year
    /// </summary>
    public decimal TotalDiscountedCashflows { get; init; }

    /// <summary>
    /// Gets the total portfolio loss for this year
    /// </summary>
    public decimal TotalPortfolioLoss { get; init; }

    /// <summary>
    /// Gets the average LGD for this year
    /// </summary>
    public decimal AverageLgd { get; init; }

    /// <summary>
    /// Gets the total records processed for this year
    /// </summary>
    public int TotalRecordsProcessed { get; init; }
}