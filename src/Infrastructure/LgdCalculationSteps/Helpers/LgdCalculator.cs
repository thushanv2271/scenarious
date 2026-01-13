namespace Infrastructure.LgdCalculationSteps.Helpers;

/// <summary>
/// Calculator for LGD (Loss Given Default) calculations
/// </summary>
internal static class LgdCalculator
{
    /// <summary>
    /// Calculates LGD (Loss Given Default) as a percentage using the formula: LGD = 100 - Recovery Rate.
    /// </summary>
    /// <param name="recoveryRatePercentage">Recovery rate as percentage (0 to 100, where 100 = 100% recovery)</param>
    /// <returns>LGD percentage value (0 to 100, where 100 = 100% loss)</returns>
    public static decimal CalculateLgd(decimal recoveryRatePercentage)
    {
        // Ensure recovery rate percentage is within valid bounds
        decimal validRecoveryRatePercentage = Math.Max(0m, Math.Min(100m, recoveryRatePercentage));

        // LGD = 100 - Recovery Rate (both as percentages)
        return 100m - validRecoveryRatePercentage;
    }




}