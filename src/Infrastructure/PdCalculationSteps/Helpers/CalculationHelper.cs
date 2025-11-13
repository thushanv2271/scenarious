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
    /// <returns>Remaining maturity in appropriate units (0 if maturity date is before quarter end date)</returns>
    public static int CalculateRemainingMaturity(DateTime maturityDate, DateTime quarterEndDate, FrequencyType frequency)
    {
        // Handle invalid dates
        if (maturityDate == DateTime.MinValue || quarterEndDate == DateTime.MinValue)
        {
            return 0;
        }

        if (maturityDate <= quarterEndDate)
        {
            return 0;
        }

        double daysDifference = (maturityDate - quarterEndDate).TotalDays;

        int divisor = frequency switch
        {
            FrequencyType.Yearly => 365,      // Days in a year
            FrequencyType.Quarterly => 90,    // Days in a quarter (approximately)
            FrequencyType.Monthly => 30,      // Days in a month (approximately)
            _ => throw new ArgumentException($"Unknown frequency type: {frequency}")
        };

        return (int)Math.Round(daysDifference / divisor, 0, MidpointRounding.AwayFromZero);
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
