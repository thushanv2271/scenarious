namespace Infrastructure.LgdCalculationSteps.Helpers;

/// <summary>
/// Calculator for Recovery Rate calculations
/// </summary>
internal static class LgdRecoveryRateCalculator
{
    /// <summary>
    /// Calculates recovery rate as a percentage with Excel-like error handling.
    /// Replicates Excel formula: =IF([Facility Number]="","",IFERROR(IF([Sum of Discounted Cashflows]/[Max of Total Outstanding as at First NPL Date]>1,1,[Sum of Discounted Cashflows]/[Max of Total Outstanding as at First NPL Date]),0))
    /// </summary>
    /// <param name="totalDiscountedCashflows">Sum of discounted cashflows</param>
    /// <param name="maxTotalOutstandingAsAtFirstNplDate">Max total outstanding as at first NPL date</param>
    /// <returns>Recovery rate as percentage (0 to 100, where 100 = 100% recovery)</returns>
    public static decimal CalculateRecoveryRate(decimal totalDiscountedCashflows, decimal maxTotalOutstandingAsAtFirstNplDate)
    {
        try
        {
            // Handle division by zero case (equivalent to IFERROR returning 0)
            if (maxTotalOutstandingAsAtFirstNplDate == 0m)
            {
                return 0m;
            }

            // Calculate raw recovery rate
            decimal rawRecoveryRate = totalDiscountedCashflows / maxTotalOutstandingAsAtFirstNplDate;

            // Cap at 1 (100% recovery rate maximum) and convert to percentage
            return Math.Min(rawRecoveryRate, 1m) * 100m;
        }
        catch (Exception)
        {
            // IFERROR equivalent - return 0 on any calculation error
            return 0m;
        }
    }


}