using Domain.LGDCalculation;

namespace Infrastructure.LgdCalculationSteps.Helpers;

/// <summary>
/// Calculator for determining VC closed status for facilities in VC_LGD calculations
/// </summary>
internal static class VCClosedStatusCalculator
{
    /// <summary>
    /// Calculates the VC closed status and maximum DPD for a facility based on maximum DPD vs VC_POINT threshold
    /// </summary>
    /// <param name="facilityGroup">Grouped LGD details for a single facility</param>
    /// <param name="vcPoint">The VC_POINT threshold value (in years)</param>
    /// <returns>Tuple containing the maximum DPD and whether facility is considered VC closed</returns>
    /// <exception cref="ArgumentNullException">Thrown when facilityGroup is null</exception>
    /// <exception cref="ArgumentException">Thrown when vcPoint is negative</exception>
    public static (decimal MaxDaysPastDue, bool IsVCClosed) CalculateVCStatusAndMaxDpd(IGrouping<string, LgdDetails> facilityGroup, decimal vcPoint)
    {
        if (facilityGroup is null)
        {
            throw new ArgumentNullException(nameof(facilityGroup), "Facility group cannot be null");
        }

        if (vcPoint < 0)
        {
            throw new ArgumentException("VC Point cannot be negative", nameof(vcPoint));
        }

        try
        {
            // Get the maximum Days Past Due for this facility
            decimal maxDaysPastDue = facilityGroup
                .Select(detail => detail.DaysPastDue)
                .DefaultIfEmpty(0)
                .Max();

            // Convert VC_POINT from years to days threshold
            decimal vcPointThresholdInDays = vcPoint * 365;

            // Determine if facility is VC closed
            // Logic: If maximum DPD > (VC_POINT * 365), then true; otherwise false
            bool isVCClosed = maxDaysPastDue > vcPointThresholdInDays;

            return (maxDaysPastDue, isVCClosed);
        }
        catch (Exception)
        {
            // Return safe defaults for any calculation errors
            return (0m, false);
        }
    }

    /// <summary>
    /// Calculates the maximum Days Past Due for a facility
    /// </summary>
    /// <param name="facilityGroup">Grouped LGD details for a single facility</param>
    /// <returns>Maximum Days Past Due value</returns>
    /// <exception cref="ArgumentNullException">Thrown when facilityGroup is null</exception>
    public static decimal CalculateMaxDaysPastDue(IGrouping<string, LgdDetails> facilityGroup)
    {
        if (facilityGroup is null)
        {
            throw new ArgumentNullException(nameof(facilityGroup), "Facility group cannot be null");
        }

        try
        {
            // Get the maximum Days Past Due for this facility
            decimal maxDaysPastDue = facilityGroup
                .Select(detail => detail.DaysPastDue)
                .DefaultIfEmpty(0)
                .Max();

            return maxDaysPastDue;
        }
        catch (Exception)
        {
            // Return safe default for any calculation errors
            return 0m;
        }
    }
}