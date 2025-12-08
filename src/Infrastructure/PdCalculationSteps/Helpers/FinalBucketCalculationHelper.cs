using Application.Models;
using Domain.PDCalculation;
using System.Text.RegularExpressions;

namespace Infrastructure.PDCalculationSteps.Helpers;

/// <summary>
/// Helper class for calculating final bucket based on customer grouping and sorting logic
/// </summary>
public static class FinalBucketCalculationHelper
{
    /// <summary>
    /// Calculates the final bucket for all loan records based on customer grouping and payload logic
    /// </summary>
    /// <param name="loanDetails">List of loan details for a specific period</param>
    /// <param name="finalBucketPayload">Payload configuration for final bucket calculation</param>
    /// <returns>Dictionary mapping loan detail ID to final bucket</returns>
    public static Dictionary<Guid, string> CalculateFinalBuckets(
        List<LoanDetails> loanDetails,
        FinalBucketPayload finalBucketPayload)
    {
        var finalBuckets = new Dictionary<Guid, string>();

        if (!finalBucketPayload.IsValid() || !loanDetails.Any())
        {
            // Set all to their original bucket if no payload or no data
            ApplyOriginalBuckets(loanDetails, finalBuckets);
            return finalBuckets;
        }

        // Normalize the type ONCE
        string type = finalBucketPayload.Type?.Trim() ?? string.Empty;

        // Group by customer number and period
        IEnumerable<IGrouping<string, LoanDetails>> customerPeriodGroups = loanDetails
            .GroupBy(l => $"{l.CustomerNumber}|{l.Period}");

        foreach (IGrouping<string, LoanDetails> customerPeriodGroup in customerPeriodGroups)
        {
            var customerLoans = customerPeriodGroup.ToList();

            if (type.Equals(FinalBucketTypes.Worst, StringComparison.OrdinalIgnoreCase))
            {
                CalculateWorstBucket(customerLoans, finalBuckets);
            }
            else if (type.Equals(FinalBucketTypes.Percentage, StringComparison.OrdinalIgnoreCase)
                     && finalBucketPayload.Percentage.HasValue)
            {
                CalculatePercentageBucket(customerLoans, (double)finalBucketPayload.Percentage.Value, finalBuckets);
            }
            else if (type.Equals(FinalBucketTypes.None, StringComparison.OrdinalIgnoreCase))
            {
                ApplyOriginalBuckets(customerLoans, finalBuckets);
            }
            else
            {
                ApplyOriginalBuckets(customerLoans, finalBuckets);
            }

        }

        return finalBuckets;
    }

    /// <summary>
    /// Moves all loans to their original bucket.
    /// Used for NONE, invalid cases, and fallbacks.
    /// </summary>
    private static void ApplyOriginalBuckets(
        IEnumerable<LoanDetails> loans,
        Dictionary<Guid, string> finalBuckets)
    {
        foreach (LoanDetails loan in loans)
        {
            finalBuckets[loan.Id] = loan.BucketLabel;
            loan.FinalBucket = loan.BucketLabel;
        }
    }

    /// <summary>
    /// Calculates final bucket using "worst" logic (highest bucket number)
    /// </summary>
    private static void CalculateWorstBucket(List<LoanDetails> customerLoans, Dictionary<Guid, string> finalBuckets)
    {
        // Sort by bucket number in descending order (worst bucket first)
        var sortedLoans = customerLoans
            .OrderByDescending(l => ExtractBucketNumber(l.BucketLabel))
            .ToList();

        if (sortedLoans.Any())
        {
            // The worst bucket is the first one after sorting (highest number)
            string worstBucket = sortedLoans[0].BucketLabel;

            // Assign worst bucket to all loans of this customer
            foreach (LoanDetails loan in customerLoans)
            {
                finalBuckets[loan.Id] = worstBucket;
                loan.FinalBucket = worstBucket; // Update the entity property
            }
        }
    }

    /// <summary>
    /// Calculates final bucket using percentage logic with cumulative total OS.
    /// When cumulative percentage exceeds threshold, the first exceeding bucket becomes 
    /// the final bucket for all subsequent exceeding buckets.
    /// </summary>
    private static void CalculatePercentageBucket(List<LoanDetails> customerLoans, double targetPercentage, Dictionary<Guid, string> finalBuckets)
    {
        // Sort by bucket number in descending order (worst bucket first)
        var sortedLoans = customerLoans
            .OrderByDescending(l => ExtractBucketNumber(l.BucketLabel))
            .ToList();

        if (!sortedLoans.Any())
        {
            return;
        }

        // Calculate grand total of TotalOS
        decimal grandTotal = sortedLoans.Sum(l => l.TotalOS);

        if (grandTotal == 0)
        {
            // If no total OS, assign original buckets
            foreach (LoanDetails loan in customerLoans)
            {
                finalBuckets[loan.Id] = loan.BucketLabel;
                loan.FinalBucket = loan.BucketLabel; // Update the entity property
            }
            return;
        }

        // Calculate cumulative percentages and determine the first threshold exceeding bucket
        decimal cumulativeSum = 0;
        string? firstThresholdExceedingBucket = null;

        foreach (LoanDetails? loan in sortedLoans)
        {
            cumulativeSum += loan.TotalOS;
            double cumulativePercentage = (double)(cumulativeSum / grandTotal * 100);

            if (cumulativePercentage > targetPercentage)
            {
                // This is the first bucket to exceed the threshold
                firstThresholdExceedingBucket = loan.BucketLabel;
                break;
            }
        }

        // Assign final buckets based on the new logic
        cumulativeSum = 0;
        foreach (LoanDetails? loan in sortedLoans)
        {
            cumulativeSum += loan.TotalOS;
            double cumulativePercentage = (double)(cumulativeSum / grandTotal * 100);

            if (cumulativePercentage <= targetPercentage)
            {
                // Within range: use their own bucket
                finalBuckets[loan.Id] = loan.BucketLabel;
                loan.FinalBucket = loan.BucketLabel; // Update the entity property
            }
            else
            {
                // Exceeds range: use first threshold exceeding bucket if available, otherwise use own bucket
                string bucketToUse = firstThresholdExceedingBucket ?? loan.BucketLabel;
                finalBuckets[loan.Id] = bucketToUse;
                loan.FinalBucket = bucketToUse; // Update the entity property
            }
        }
    }

    /// <summary>
    /// Extracts the bucket number from bucket label (e.g., "Age Bucket 5" -> 5)
    /// </summary>
    private static int ExtractBucketNumber(string bucketLabel)
    {
        if (string.IsNullOrWhiteSpace(bucketLabel))
        {
            return 0;
        }

        // Use regex to extract the number from the bucket label
        Match match = Regex.Match(bucketLabel, @"\d+");
        return match.Success ? int.Parse(match.Value, System.Globalization.CultureInfo.InvariantCulture) : 0;
    }
}
