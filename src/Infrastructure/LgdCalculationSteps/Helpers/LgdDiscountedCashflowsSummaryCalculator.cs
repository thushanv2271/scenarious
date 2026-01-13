using Application.DTOs.LGDCalculation;
using Application.Models;
using Domain.LGDCalculation;

namespace Infrastructure.LgdCalculationSteps.Helpers;

/// <summary>
/// Main coordinator for calculating sum of discounted cashflows grouped by facility number
/// </summary>
internal static class LgdDiscountedCashflowsSummaryCalculator
{


    /// <summary>
    /// Calculates the sum of discounted cashflows grouped by FacilityNumber with additional metrics.
    /// </summary>
    /// <param name="lgdDetails">Collection of LGD details to process</param>
    /// <param name="calculationType">Type of calculation being performed (LGD or VC_LGD)</param>
    /// <param name="vcPoint">Optional VC Point threshold value in years. Only used for VC_LGD calculations.</param>
    /// <returns>Dictionary with FacilityNumber as key and summary information as value</returns>
    /// <exception cref="ArgumentNullException">Thrown when lgdDetails is null</exception>
    public static Dictionary<string, FacilityDiscountedCashflowSummary> CalculateDetailedSumOfDiscountedCashflows(
        IEnumerable<LgdDetails> lgdDetails,
        LgdCalculationType calculationType = LgdCalculationType.LGD,
        decimal? vcPoint = null)
    {
        if (lgdDetails is null)
        {
            throw new ArgumentNullException(nameof(lgdDetails), "LGD details collection cannot be null");
        }

        try
        {
            var facilitySummaries = lgdDetails
                .Where(detail => !string.IsNullOrWhiteSpace(detail.FacilityNumber)) // Filter out null/empty facility numbers
                .GroupBy(
                    detail => detail.FacilityNumber,
                    StringComparer.OrdinalIgnoreCase) // Case-insensitive grouping for facility numbers
                .ToDictionary(
                    group => group.Key,
                    group => CalculateFacilitySummary(group, calculationType, vcPoint),
                    StringComparer.OrdinalIgnoreCase);

            return facilitySummaries;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error occurred while calculating detailed sum of discounted cashflows: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Calculates facility summary with all metrics using separate specialized calculators
    /// </summary>
    /// <param name="facilityGroup">Grouped LGD details for a single facility</param>
    /// <param name="calculationType">Type of calculation being performed (LGD or VC_LGD)</param>
    /// <param name="vcPoint">Optional VC Point threshold value in years. Only used for VC_LGD calculations.</param>
    /// <returns>Complete facility summary</returns>
    private static FacilityDiscountedCashflowSummary CalculateFacilitySummary(
        IGrouping<string, LgdDetails> facilityGroup,
        LgdCalculationType calculationType,
        decimal? vcPoint)
    {
        // Use specialized calculators for each metric
        decimal totalDiscountedCashflows = LgdSumDiscountedCashflowsCalculator.CalculateSumOfDiscountedCashflows(facilityGroup);
        decimal maxTotalOutstandingAsAtFirstNplDate = LgdMaxTotalOutstandingCalculator.CalculateMaxTotalOutstandingAsAtFirstNplDate(facilityGroup);
        decimal recoveryRate = LgdRecoveryRateCalculator.CalculateRecoveryRate(totalDiscountedCashflows, maxTotalOutstandingAsAtFirstNplDate);
        decimal lgd = LgdCalculator.CalculateLgd(recoveryRate);
        decimal loss = LgdLossCalculator.CalculateLoss(maxTotalOutstandingAsAtFirstNplDate, lgd);
        DiscountedCashflowStatistics statistics = LgdSumDiscountedCashflowsCalculator.CalculateStatistics(facilityGroup);

        // Calculate years from NPL date to closure date (using MaturityDate as closure proxy)
        decimal yearsFromNplToClosureDate = CalculateYearsFromNplToClosureDate(facilityGroup);

        // Calculate maximum Days Past Due and VC Closed status for VC_LGD calculations
        decimal maxDaysPastDue = VCClosedStatusCalculator.CalculateMaxDaysPastDue(facilityGroup);
        bool? isVCClosed = null;

        if (calculationType == LgdCalculationType.VC_LGD && vcPoint.HasValue)
        {
            (maxDaysPastDue, bool vcClosedStatus) = VCClosedStatusCalculator.CalculateVCStatusAndMaxDpd(facilityGroup, vcPoint.Value);
            isVCClosed = vcClosedStatus;
        }

        // Apply VC Closed logic: if facility is VC Closed, override certain values
        if (isVCClosed == true)
        {
            return new FacilityDiscountedCashflowSummary
            {
                FacilityNumber = facilityGroup.Key,
                TotalDiscountedCashflows = null, // Set to null for VC Closed facilities
                RecordCount = statistics.Count,
                MinDiscountedCashflow = statistics.Min,
                MaxDiscountedCashflow = statistics.Max,
                AverageDiscountedCashflow = statistics.Average,
                MaxTotalOutstandingAsAtFirstNplDate = null, // Set to null for VC Closed facilities
                RecoveryRate = 0m, // Set to 0 for VC Closed facilities
                Lgd = 100m, // Set to 100 for VC Closed facilities
                Loss = null, // Set to null for VC Closed facilities
                YearsFromNplToClosureDate = yearsFromNplToClosureDate,
                MaxDaysPastDue = maxDaysPastDue,
                IsVCClosed = isVCClosed
            };
        }

        return new FacilityDiscountedCashflowSummary
        {
            FacilityNumber = facilityGroup.Key,
            TotalDiscountedCashflows = statistics.Sum,
            RecordCount = statistics.Count,
            MinDiscountedCashflow = statistics.Min,
            MaxDiscountedCashflow = statistics.Max,
            AverageDiscountedCashflow = statistics.Average,
            MaxTotalOutstandingAsAtFirstNplDate = maxTotalOutstandingAsAtFirstNplDate,
            RecoveryRate = recoveryRate,
            Lgd = lgd,
            Loss = loss,
            YearsFromNplToClosureDate = yearsFromNplToClosureDate,
            MaxDaysPastDue = maxDaysPastDue,
            IsVCClosed = isVCClosed
        };
    }

    /// <summary>
    /// Calculates years from NPL date to closure date for a facility group.
    /// Uses the actual Closure Date from the database for accurate calculation.
    /// </summary>
    /// <param name="facilityGroup">Grouped LGD details for a single facility</param>
    /// <returns>Years from NPL date to closure date</returns>
    private static decimal CalculateYearsFromNplToClosureDate(IGrouping<string, LgdDetails> facilityGroup)
    {
        try
        {
            // Get the first NPL date for this facility (should be consistent across records)
            DateTime? firstNplDate = facilityGroup
                .Select(detail => detail.FirstNplDate)
                .FirstOrDefault(date => date.HasValue);

            // Use the actual Closure Date from the database
            DateTime closureDate = facilityGroup
                .Select(detail => detail.ClosureDate)
                .FirstOrDefault(date => date != default);

            // Fallback: If no closure date is available, use the maturity date
            if (closureDate == default)
            {
                closureDate = facilityGroup
                    .Select(detail => detail.MaturityDate)
                    .FirstOrDefault();
            }

            // Final fallback: If no maturity date is available, use the latest receipt date
            if (closureDate == default)
            {
                DateTime maxReceiptDate = facilityGroup
                    .Select(detail => detail.ReceiptDate)
                    .DefaultIfEmpty(DateTime.MinValue)
                    .Max();

                closureDate = maxReceiptDate == DateTime.MinValue ? default : maxReceiptDate;
            }

            // Convert to nullable DateTime for the calculator
            DateTime? nullableClosureDate = closureDate == default ? null : closureDate;

            // Use the specialized calculator
            return LgdYearsToClosureCalculator.CalculateYearsToClosureFromNpl(nullableClosureDate, firstNplDate);
        }
        catch (Exception)
        {
            // Return 0 for any calculation errors
            return 0m;
        }
    }
}