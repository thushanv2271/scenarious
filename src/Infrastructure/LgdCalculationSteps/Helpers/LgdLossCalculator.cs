namespace Infrastructure.LgdCalculationSteps.Helpers;

/// <summary>
/// Calculator for LOSS calculations
/// </summary>
internal static class LgdLossCalculator
{
    /// <summary>
    /// Calculates LOSS using the formula: LOSS = Max Total Outstanding as at First NPL Date * (LGD / 100).
    /// Replicates Excel formula: =IF([Facility Number]="","",[Max of Total Outstanding as at First NPL Date]*[LGD])
    /// </summary>
    /// <param name="maxTotalOutstandingAsAtFirstNplDate">Max total outstanding as at first NPL date</param>
    /// <param name="lgdPercentage">LGD (Loss Given Default) rate as percentage (0-100)</param>
    /// <returns>Calculated loss amount</returns>
    public static decimal CalculateLoss(decimal maxTotalOutstandingAsAtFirstNplDate, decimal lgdPercentage)
    {
        try
        {
            // Handle edge cases
            if (maxTotalOutstandingAsAtFirstNplDate < 0m)
            {
                maxTotalOutstandingAsAtFirstNplDate = 0m;
            }

            if (lgdPercentage < 0m)
            {
                lgdPercentage = 0m;
            }
            else if (lgdPercentage > 100m)
            {
                lgdPercentage = 100m;
            }

            // Convert percentage to decimal for calculation: LOSS = Max Total Outstanding * (LGD / 100)
            return maxTotalOutstandingAsAtFirstNplDate * (lgdPercentage / 100m);
        }
        catch (Exception)
        {
            // Return 0 on any calculation error (Excel-like error handling)
            return 0m;
        }
    }
}
