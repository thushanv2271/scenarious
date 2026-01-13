namespace Infrastructure.LgdCalculationSteps.Helpers;

/// <summary>
/// Helper class for calculating discount factors (DCF) in LGD calculations
/// </summary>
internal static class LgdDiscountFactorCalculator
{
    /// <summary>
    /// Calculates the discount factor (DCF) based on the Excel formula:
    /// =IF([Facility Number]="","",IF([Receipt Date]<[First NPL Date],0,1/(1+[Interest Rate]/100)^(([Receipt Date]-[First NPL Date])/365)))
    /// </summary>
    /// <param name="facilityNumber">Facility Number</param>
    /// <param name="receiptDate">Receipt Date</param>
    /// <param name="firstNplDate">First NPL Date (nullable)</param>
    /// <param name="interestRate">Interest Rate (as decimal, e.g., 0.10 for 10%)</param>
    /// <returns>Calculated discount factor (DCF)</returns>
    public static decimal CalculateDiscountFactor(
        string facilityNumber,
        DateTime receiptDate,
        DateTime? firstNplDate,
        decimal interestRate)
    {
        // If Facility Number is empty, return 0
        if (string.IsNullOrWhiteSpace(facilityNumber))
        {
            return 0m;
        }

        // If First NPL Date is null or not provided, return 0
        if (!firstNplDate.HasValue)
        {
            return 0m;
        }

        // If Receipt Date is before First NPL Date, return 0
        if (receiptDate < firstNplDate.Value)
        {
            return 0m;
        }

        try
        {
            // Calculate the number of days between Receipt Date and First NPL Date
            double daysDifference = (receiptDate - firstNplDate.Value).TotalDays;

            // Calculate the discount factor: 1/(1+InterestRate/100)^(daysDifference/365)
            // Note: The interest rate is already in decimal form (0.10 for 10%), but the formula expects percentage
            // So we multiply by 100 to convert back to percentage for the calculation
            decimal interestRatePercentage = interestRate * 100;

            // Calculate: 1 + (InterestRate/100)
            double baseRate = 1.0 + (double)(interestRatePercentage / 100);

            // Calculate the exponent: daysDifference/365
            double exponent = daysDifference / 365.0;

            // Calculate: baseRate^exponent
            double powerResult = Math.Pow(baseRate, exponent);

            // Calculate: 1 / powerResult
            decimal discountFactor = (decimal)(1.0 / powerResult);

            return discountFactor;
        }
        catch (Exception)
        {
            // In case of any mathematical errors (overflow, etc.), return 0
            return 0m;
        }
    }
}