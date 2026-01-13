using Application.Models;
using Domain.PDCalculation;

namespace Infrastructure.PDCalculationSteps.Helpers;

/// <summary>
/// Helper class for PD calculations
/// </summary>
public static class CalculationHelper
{
    /// <summary>
    /// Calculates remaining maturity in the appropriate period units (years/quarters/months)
    /// </summary>
    /// <param name="maturityDate">The maturity date</param>
    /// <param name="quarterEndDate">The quarter end date</param>
    /// <param name="frequency">The frequency type for determining calculation period</param>
    /// <param name="fileName">The name of the file being processed (for logging/tracking purposes)</param>
    /// <param name="isLatestPortfolio">True if this file is part of the latest portfolio</param>
    /// <returns>Remaining maturity in appropriate units (0 if maturity date is before quarter end date)</returns>
    public static int CalculateRemainingMaturity(DateTime maturityDate, DateTime quarterEndDate, FrequencyType frequency, string fileName, bool isLatestPortfolio)
    {
        // Handle invalid dates
        if (maturityDate == DateTime.MinValue || quarterEndDate == DateTime.MinValue)
        {
            return isLatestPortfolio ? 0 : 100;
        }

        // Log the file being processed for debugging purposes
        // Note: Using Console.WriteLine since this is a static helper without logger injection
        string portfolioStatus = isLatestPortfolio ? "[LATEST PORTFOLIO]" : "[OLDER PORTFOLIO]";
        Console.WriteLine($"[CalculateRemainingMaturity] {portfolioStatus} Processing file: {fileName}");


        double daysDifference = (maturityDate - quarterEndDate).TotalDays;

        // Match Excel divisors (365, 365/4, 365/12)
        double divisor;
        switch (frequency)
        {
            case FrequencyType.Yearly:
                {
                    divisor = 365.0;
                    break;
                }
            case FrequencyType.Quarterly:
                {
                    divisor = 365.0 / 4.0;    // 91.25 days per quarter
                    break;
                }
            case FrequencyType.Monthly:
                {
                    divisor = 365.0 / 12.0;   // ~30.4167 days per month
                    break;
                }
            default:
                {
                    throw new ArgumentException($"Unknown frequency type: {frequency}");
                }
        }

        double units = daysDifference / divisor;

        // Excel ROUNDUP(number, 0): always away from zero
        int roundedUpAwayFromZero;
        if (units > 0)
        {
            roundedUpAwayFromZero = (int)Math.Ceiling(units);
        }
        else if (units < 0)
        {
            // Math.Floor returns the largest integer <= the value
            roundedUpAwayFromZero = (int)Math.Floor(units);
        }
        else
        {
            roundedUpAwayFromZero = 0;
        }

        // If negative, return default value based on frequency; otherwise return the rounded value
        if (roundedUpAwayFromZero < 0)
        {
            return isLatestPortfolio ? 0 : 100;
        }

        return roundedUpAwayFromZero;
    }



    /// <summary>
    /// Determines the bucket label based on days past due and bucket configurations
    /// </summary>
    /// <param name="daysPastDue">Days past due value</param>
    /// <param name="buckets">List of date passed due bucket configurations</param>
    /// <returns>Bucket label for the given days past due</returns>
    public static string DetermineBucketLabel(int daysPastDue, List<DatePassedDueBucket> buckets)
    {
        if (buckets == null || buckets.Count == 0)
        {
            return "Unknown Bucket";
        }

        // Find the first bucket that contains the daysPastDue value
        DatePassedDueBucket? matchingBucket = buckets.FirstOrDefault(bucket => bucket.IsInRange(daysPastDue));

        if (matchingBucket is not null)
        {
            return matchingBucket.BucketLabel;
        }

        // If no bucket matches and daysPastDue exceeds all ranges, use the last bucket
        // Sort buckets by range end to get the highest range bucket
        DatePassedDueBucket lastBucket = buckets.OrderBy(b => b.RangeEndInt).Last();

        // Check if daysPastDue is higher than the last bucket's range
        if (daysPastDue > lastBucket.RangeEndInt)
        {
            return lastBucket.BucketLabel;
        }

        return "Unknown Bucket";
    }
}
