using Domain.LGDCalculation;

namespace Infrastructure.LgdCalculationSteps.Helpers;

/// <summary>
/// Calculator for Sum of Discounted Cashflows
/// </summary>
internal static class LgdSumDiscountedCashflowsCalculator
{
    /// <summary>
    /// Calculates the sum of discounted cashflows for a facility group.
    /// Replicates Excel formula: =SUMIFS(DiscountedCashflows, FacilityNumber, UniqueFacilityNumber)
    /// </summary>
    /// <param name="facilityDetails">Collection of LGD details for a specific facility</param>
    /// <returns>Sum of discounted cashflows</returns>
    /// <exception cref="ArgumentNullException">Thrown when facilityDetails is null</exception>
    public static decimal CalculateSumOfDiscountedCashflows(IEnumerable<LgdDetails> facilityDetails)
    {
        if (facilityDetails is null)
        {
            throw new ArgumentNullException(nameof(facilityDetails), "Facility details collection cannot be null");
        }

        try
        {
            return facilityDetails.Sum(detail => detail.DiscountedCashflows);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error occurred while calculating sum of discounted cashflows: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Calculates the sum of discounted cashflows grouped by FacilityNumber.
    /// </summary>
    /// <param name="lgdDetails">Collection of all LGD details</param>
    /// <returns>Dictionary with FacilityNumber as key and sum of DiscountedCashflows as value</returns>
    /// <exception cref="ArgumentNullException">Thrown when lgdDetails is null</exception>
    public static Dictionary<string, decimal> CalculateSumOfDiscountedCashflowsByFacility(IEnumerable<LgdDetails> lgdDetails)
    {
        if (lgdDetails is null)
        {
            throw new ArgumentNullException(nameof(lgdDetails), "LGD details collection cannot be null");
        }

        try
        {
            var facilitySums = lgdDetails
                .Where(detail => !string.IsNullOrWhiteSpace(detail.FacilityNumber)) // Filter out null/empty facility numbers
                .GroupBy(
                    detail => detail.FacilityNumber,
                    StringComparer.OrdinalIgnoreCase) // Case-insensitive grouping for facility numbers
                .ToDictionary(
                    group => group.Key,
                    group => CalculateSumOfDiscountedCashflows(group),
                    StringComparer.OrdinalIgnoreCase);

            return facilitySums;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error occurred while calculating sum of discounted cashflows by facility: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Calculates statistical metrics for discounted cashflows of a facility group.
    /// </summary>
    /// <param name="facilityDetails">Collection of LGD details for a specific facility</param>
    /// <returns>Statistical metrics including min, max, average, and count</returns>
    /// <exception cref="ArgumentNullException">Thrown when facilityDetails is null</exception>
    public static DiscountedCashflowStatistics CalculateStatistics(IEnumerable<LgdDetails> facilityDetails)
    {
        if (facilityDetails is null)
        {
            throw new ArgumentNullException(nameof(facilityDetails), "Facility details collection cannot be null");
        }

        try
        {
            var cashflows = facilityDetails.Select(detail => detail.DiscountedCashflows).ToList();

            if (cashflows.Count == 0)
            {
                return new DiscountedCashflowStatistics
                {
                    Sum = 0m,
                    Min = 0m,
                    Max = 0m,
                    Average = 0m,
                    Count = 0
                };
            }

            return new DiscountedCashflowStatistics
            {
                Sum = cashflows.Sum(),
                Min = cashflows.Min(),
                Max = cashflows.Max(),
                Average = cashflows.Average(),
                Count = cashflows.Count
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error occurred while calculating discounted cashflows statistics: {ex.Message}",
                ex);
        }
    }
}

/// <summary>
/// Statistical metrics for discounted cashflows
/// </summary>
public sealed record DiscountedCashflowStatistics
{
    /// <summary>
    /// Sum of all discounted cashflows
    /// </summary>
    public decimal Sum { get; init; }

    /// <summary>
    /// Minimum discounted cashflow value
    /// </summary>
    public decimal Min { get; init; }

    /// <summary>
    /// Maximum discounted cashflow value
    /// </summary>
    public decimal Max { get; init; }

    /// <summary>
    /// Average discounted cashflow value
    /// </summary>
    public decimal Average { get; init; }

    /// <summary>
    /// Number of records
    /// </summary>
    public int Count { get; init; }
}