namespace Domain.EclAnalysis;

/// <summary>
/// Types of threshold calculation methods
/// </summary>
public enum ThresholdType
{
    /// <summary>
    /// Custom absolute threshold amount
    /// </summary>
    CustomAbsolute = 0,

    /// <summary>
    /// Top N customers by exposure
    /// </summary>
    TopNCustomers = 1,

    /// <summary>
    /// Cumulative percentage of portfolio
    /// </summary>
    CumulativePercentage = 2
}
