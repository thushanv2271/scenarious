using Domain.LGDCalculation;

namespace Infrastructure.LgdCalculationSteps.Helpers;

/// <summary>
/// Calculator for Max of Total Outstanding as at First NPL Date
/// </summary>
internal static class LgdMaxTotalOutstandingCalculator
{
    /// <summary>
    /// Calculates the maximum of Total Outstanding as at First NPL Date for a facility group.
    /// Replicates Excel formula: =IF([Facility Number]="","",MAX(MAXIFS([Total Outstanding as at First NPL Date],[Facility Number],[Facility Number]),0))
    /// </summary>
    /// <param name="facilityDetails">Collection of LGD details for a specific facility</param>
    /// <returns>Maximum total outstanding as at first NPL date, or 0 if no valid data</returns>
    /// <exception cref="ArgumentNullException">Thrown when facilityDetails is null</exception>
    public static decimal CalculateMaxTotalOutstandingAsAtFirstNplDate(IEnumerable<LgdDetails> facilityDetails)
    {
        if (facilityDetails is null)
        {
            throw new ArgumentNullException(nameof(facilityDetails), "Facility details collection cannot be null");
        }

        try
        {
            // Convert to list to avoid multiple enumerations
            var detailsList = facilityDetails.ToList();

            // If no items, return 0
            if (detailsList.Count == 0)
            {
                return 0m;
            }

            // Calculate maximum of TotalOutstandingAsAtFirstNplDate values, with minimum of 0
            decimal maxValue = detailsList.Max(detail => detail.TotalOutstandingAsAtFirstNplDate);
            return Math.Max(maxValue, 0m);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error occurred while calculating max total outstanding as at first NPL date: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Calculates the maximum of Total Outstanding as at First NPL Date for multiple facilities grouped by FacilityNumber.
    /// </summary>
    /// <param name="lgdDetails">Collection of all LGD details</param>
    /// <returns>Dictionary with FacilityNumber as key and max total outstanding as value</returns>
    /// <exception cref="ArgumentNullException">Thrown when lgdDetails is null</exception>
    public static Dictionary<string, decimal> CalculateMaxTotalOutstandingByFacility(IEnumerable<LgdDetails> lgdDetails)
    {
        if (lgdDetails is null)
        {
            throw new ArgumentNullException(nameof(lgdDetails), "LGD details collection cannot be null");
        }

        try
        {
            var facilityMaxOutstanding = lgdDetails
                .Where(detail => !string.IsNullOrWhiteSpace(detail.FacilityNumber))
                .GroupBy(
                    detail => detail.FacilityNumber,
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => CalculateMaxTotalOutstandingAsAtFirstNplDate(group),
                    StringComparer.OrdinalIgnoreCase);

            return facilityMaxOutstanding;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error occurred while calculating max total outstanding by facility: {ex.Message}",
                ex);
        }
    }
}