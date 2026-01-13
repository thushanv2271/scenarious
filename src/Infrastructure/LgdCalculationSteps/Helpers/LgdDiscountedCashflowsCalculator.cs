namespace Infrastructure.LgdCalculationSteps.Helpers;

/// <summary>
/// Helper class for calculating discounted cashflows in LGD calculations
/// </summary>
internal static class LgdDiscountedCashflowsCalculator
{
    /// <summary>
    /// Calculates the discounted cashflows based on the Excel formula:
    /// =IF([Facility Number]="","",[Cashflows]*[DCF])
    /// </summary>
    /// <param name="facilityNumber">Facility Number</param>
    /// <param name="cashflows">Cashflow amount</param>
    /// <param name="dcf">Discount factor (DCF)</param>
    /// <returns>Calculated discounted cashflows</returns>
    public static decimal CalculateDiscountedCashflows(
        string facilityNumber,
        decimal cashflows,
        decimal dcf)
    {
        // If Facility Number is empty, return 0
        if (string.IsNullOrWhiteSpace(facilityNumber))
        {
            return 0m;
        }

        try
        {
            // Calculate: Cashflows * DCF
            decimal discountedCashflows = cashflows * dcf;
            return discountedCashflows;
        }
        catch (Exception)
        {
            // In case of any mathematical errors (overflow, etc.), return 0
            return 0m;
        }
    }
}