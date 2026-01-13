namespace Application.DTOs.LGDCalculation;

/// <summary>
/// Result of Step 2 LGD calculation containing facility summaries and totals
/// </summary>
public sealed record Step2LgdCalculationResult
{
    /// <summary>
    /// Gets the list of facility summaries with detailed information
    /// </summary>
    public List<FacilityDiscountedCashflowSummary> FacilitySummaries { get; init; } = new();

    /// <summary>
    /// Gets the total number of unique facilities processed
    /// </summary>
    public int TotalFacilities { get; init; }

    /// <summary>
    /// Gets the grand total of all discounted cashflows across all facilities
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
    /// Gets the total portfolio loss across all facilities
    /// </summary>
    public decimal TotalPortfolioLoss { get; init; }

    /// <summary>
    /// Gets the average LGD (Loss Given Default) across all facilities
    /// </summary>
    public decimal AverageLgd { get; init; }

    /// <summary>
    /// Gets the average recovery rate across all facilities
    /// </summary>
    public decimal AverageRecoveryRate { get; init; }

    /// <summary>
    /// Gets the total maximum outstanding amount across all facilities
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
}