namespace Application.CashFlowProjections.Common;

/// <summary>
/// Constants for cash flow calculations
/// </summary>
public static class CashFlowConstants
{
    /// <summary>
    /// Default haircut percentage for collateral (40%)
    /// </summary>
    public const decimal DefaultHaircutPercentage = 0.40m;

    /// <summary>
    /// Minimum tenure in months when maturity date has passed
    /// </summary>
    public const int MinimumTenureMonths = 1;

    /// <summary>
    /// Maximum records to fetch for small datasets
    /// </summary>
    public const int SmallDatasetThreshold = 10000;
}
