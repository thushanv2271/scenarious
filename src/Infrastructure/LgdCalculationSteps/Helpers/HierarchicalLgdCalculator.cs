using Application.DTOs.LGDCalculation;
using Application.Models;
using Domain.LGDCalculation;
using Infrastructure.LgdCalculationSteps.Helpers;

namespace Infrastructure.LgdCalculationSteps.Helpers;

/// <summary>
/// Calculator for hierarchical LGD calculation results organized by Year > LGD Classification (Segment) > Facility
/// </summary>
internal static class HierarchicalLgdCalculator
{
    /// <summary>
    /// Calculates hierarchical LGD results grouped by Year, then by LGD Classification (Segment), then by Facility
    /// </summary>
    /// <param name="lgdDetailsWithFileInfo">Collection of LGD details with their associated file information</param>
    /// <param name="calculationType">Type of calculation being performed (LGD or VC_LGD)</param>
    /// <param name="vcPoint">Legacy VC Point threshold value in years. Only used for VC_LGD calculations for backward compatibility.</param>
    /// <param name="vcPointsByClassification">VC Point threshold values by classification. Only used for VC_LGD calculations. Takes precedence over vcPoint parameter.</param>
    /// <returns>Hierarchical result organized by year, segment, and facility</returns>
    /// <exception cref="ArgumentNullException">Thrown when lgdDetailsWithFileInfo is null</exception>
    public static HierarchicalStep2LgdCalculationResult CalculateHierarchicalResult(
        IEnumerable<LgdDetailsWithFileInfo> lgdDetailsWithFileInfo,
        LgdCalculationType calculationType = LgdCalculationType.LGD,
        decimal? vcPoint = null,
        Dictionary<string, decimal>? vcPointsByClassification = null)
    {
        if (lgdDetailsWithFileInfo is null)
        {
            throw new ArgumentNullException(nameof(lgdDetailsWithFileInfo), "LGD details with file info collection cannot be null");
        }

        try
        {
            var detailsList = lgdDetailsWithFileInfo.ToList();
            var yearClassifications = new List<YearLgdClassification>();
            var yearSummaries = new List<YearSummaryStatistics>();

            // Group by year first
            IOrderedEnumerable<IGrouping<int, LgdDetailsWithFileInfo>> yearGroups = detailsList
                .Where(detail => !string.IsNullOrWhiteSpace(detail.LgdDetails.FacilityNumber))
                .GroupBy(detail => detail.Year)
                .OrderBy(group => group.Key);

            // Create a comprehensive VC points lookup that handles both new and legacy modes
            Dictionary<string, decimal>? vcPointsLookup = null;
            if (calculationType == LgdCalculationType.VC_LGD)
            {
                if (vcPointsByClassification is not null && vcPointsByClassification.Count > 0)
                {
                    // Use classification-specific VC points (case-insensitive lookup)
                    vcPointsLookup = new Dictionary<string, decimal>(vcPointsByClassification, StringComparer.OrdinalIgnoreCase);
                }
                else if (vcPoint.HasValue)
                {
                    // Legacy mode: collect all unique segments from the data and apply the same VC point
                    var allSegments = detailsList
                        .Select(detail => detail.LgdDetails.Segment)
                        .Where(segment => !string.IsNullOrWhiteSpace(segment))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    vcPointsLookup = allSegments.ToDictionary(
                        segment => segment,
                        _ => vcPoint.Value,
                        StringComparer.OrdinalIgnoreCase);
                }
            }

            foreach (IGrouping<int, LgdDetailsWithFileInfo> yearGroup in yearGroups)
            {
                int year = yearGroup.Key;
                (YearLgdClassification yearClassification, YearSummaryStatistics yearSummary) = ProcessYearData(year, yearGroup, calculationType, vcPointsLookup);

                yearClassifications.Add(yearClassification);
                yearSummaries.Add(yearSummary);
            }

            // Calculate overall totals
            (int totalFacilities, decimal grandTotalDiscountedCashflows, decimal totalPortfolioLoss,
             decimal averageLgd, decimal averageRecoveryRate, decimal totalMaxOutstanding,
             decimal averageYearsFromNplToClosureDate, decimal minYearsFromNplToClosureDate,
             decimal maxYearsFromNplToClosureDate, int facilitiesWithZeroYearsToClosureFromNpl) = CalculateOverallTotals(yearClassifications);

            return new HierarchicalStep2LgdCalculationResult
            {
                YearClassifications = yearClassifications,
                YearSummaries = yearSummaries,
                TotalFacilities = totalFacilities,
                GrandTotalDiscountedCashflows = grandTotalDiscountedCashflows,
                TotalRecordsProcessed = detailsList.Count,
                CalculationTimestamp = DateTime.UtcNow,
                TotalPortfolioLoss = totalPortfolioLoss,
                AverageLgd = averageLgd,
                AverageRecoveryRate = averageRecoveryRate,
                TotalMaxOutstanding = totalMaxOutstanding,
                AverageYearsFromNplToClosureDate = averageYearsFromNplToClosureDate,
                MinYearsFromNplToClosureDate = minYearsFromNplToClosureDate,
                MaxYearsFromNplToClosureDate = maxYearsFromNplToClosureDate,
                FacilitiesWithZeroYearsToClosureFromNpl = facilitiesWithZeroYearsToClosureFromNpl
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error occurred while calculating hierarchical LGD results: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Processes data for a specific year
    /// </summary>
    private static (YearLgdClassification YearClassification, YearSummaryStatistics YearSummary) ProcessYearData(
        int year,
        IGrouping<int, LgdDetailsWithFileInfo> yearGroup,
        LgdCalculationType calculationType,
        Dictionary<string, decimal>? vcPointsLookup)
    {
        var lgdClassifications = new List<SegmentLgdClassification>();

        // Group by segment (LGD Classification) within the year
        IOrderedEnumerable<IGrouping<string, LgdDetailsWithFileInfo>> segmentGroups = yearGroup
            .GroupBy(detail => detail.LgdDetails.Segment, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key);

        foreach (IGrouping<string, LgdDetailsWithFileInfo> segmentGroup in segmentGroups)
        {
            string segment = segmentGroup.Key;

            // Get the VC point for this specific segment
            decimal? segmentVcPoint = null;
            if (vcPointsLookup is not null && vcPointsLookup.TryGetValue(segment, out decimal vcPointValue))
            {
                segmentVcPoint = vcPointValue;
            }

            SegmentLgdClassification segmentData = ProcessSegmentData(segment, segmentGroup.Select(x => x.LgdDetails), calculationType, segmentVcPoint);
            lgdClassifications.Add(segmentData);
        }

        // Calculate year totals
        (int totalFacilities, decimal totalDiscountedCashflows, decimal totalPortfolioLoss, decimal averageLgd) = CalculateYearTotals(lgdClassifications);

        var yearClassification = new YearLgdClassification
        {
            Year = year,
            LgdClassifications = lgdClassifications,
            TotalFacilities = totalFacilities,
            TotalDiscountedCashflows = totalDiscountedCashflows,
            TotalPortfolioLoss = totalPortfolioLoss,
            AverageLgd = averageLgd,
            TotalRecordsProcessed = yearGroup.Count()
        };

        var yearSummary = new YearSummaryStatistics
        {
            Year = year,
            UniqueSegments = lgdClassifications.Count,
            SegmentNames = lgdClassifications.Select(s => s.LgdClassification).OrderBy(x => x).ToList(),
            TotalFacilities = totalFacilities,
            TotalDiscountedCashflows = totalDiscountedCashflows,
            TotalPortfolioLoss = totalPortfolioLoss,
            AverageLgd = averageLgd,
            TotalRecordsProcessed = yearGroup.Count()
        };

        return (yearClassification, yearSummary);
    }

    /// <summary>
    /// Processes data for a specific segment within a year
    /// </summary>
    private static SegmentLgdClassification ProcessSegmentData(
        string segment,
        IEnumerable<LgdDetails> segmentDetails,
        LgdCalculationType calculationType,
        decimal? vcPoint)
    {
        Dictionary<string, FacilityDiscountedCashflowSummary> facilitySummaries = LgdDiscountedCashflowsSummaryCalculator
            .CalculateDetailedSumOfDiscountedCashflows(segmentDetails, calculationType, vcPoint);

        (decimal totalDiscountedCashflows, decimal totalPortfolioLoss, decimal averageLgd, decimal averageRecoveryRate, decimal totalMaxOutstanding) = CalculateSegmentTotals(facilitySummaries.Values);

        return new SegmentLgdClassification
        {
            LgdClassification = segment,
            FacilitySummaries = facilitySummaries.Values.OrderBy(f => f.FacilityNumber).ToList(),
            TotalFacilities = facilitySummaries.Count,
            TotalDiscountedCashflows = totalDiscountedCashflows,
            TotalPortfolioLoss = totalPortfolioLoss,
            AverageLgd = averageLgd,
            AverageRecoveryRate = averageRecoveryRate,
            TotalMaxOutstanding = totalMaxOutstanding,
            TotalRecordsProcessed = segmentDetails.Count()
        };
    }

    /// <summary>
    /// Calculates totals for a segment
    /// </summary>
    private static (
        decimal TotalDiscountedCashflows,
        decimal TotalPortfolioLoss,
        decimal AverageLgd,
        decimal AverageRecoveryRate,
        decimal TotalMaxOutstanding
    ) CalculateSegmentTotals(IEnumerable<FacilityDiscountedCashflowSummary> facilitySummaries)
    {
        var summariesList = facilitySummaries.ToList();

        if (summariesList.Count == 0)
        {
            return (0m, 0m, 0m, 0m, 0m);
        }

        return (
            TotalDiscountedCashflows: summariesList.Sum(s => s.TotalDiscountedCashflows ?? 0m), // Handle nulls for VC Closed
            TotalPortfolioLoss: summariesList.Sum(s => s.Loss ?? 0m), // Handle nulls for VC Closed
            AverageLgd: summariesList.Average(s => s.Lgd),
            AverageRecoveryRate: summariesList.Average(s => s.RecoveryRate),
            TotalMaxOutstanding: summariesList.Sum(s => s.MaxTotalOutstandingAsAtFirstNplDate ?? 0m) // Handle nulls for VC Closed
        );
    }

    /// <summary>
    /// Calculates totals for a year
    /// </summary>
    private static (
        int TotalFacilities,
        decimal TotalDiscountedCashflows,
        decimal TotalPortfolioLoss,
        decimal AverageLgd
    ) CalculateYearTotals(List<SegmentLgdClassification> segmentClassifications)
    {
        if (segmentClassifications.Count == 0)
        {
            return (0, 0m, 0m, 0m);
        }

        int totalFacilities = segmentClassifications.Sum(s => s.TotalFacilities);
        decimal totalDiscountedCashflows = segmentClassifications.Sum(s => s.TotalDiscountedCashflows);
        decimal totalPortfolioLoss = segmentClassifications.Sum(s => s.TotalPortfolioLoss);

        // Calculate weighted average LGD based on number of facilities
        decimal averageLgd = totalFacilities > 0
            ? segmentClassifications.Sum(s => s.AverageLgd * s.TotalFacilities) / totalFacilities
            : 0m;

        return (totalFacilities, totalDiscountedCashflows, totalPortfolioLoss, averageLgd);
    }

    /// <summary>
    /// Calculates overall totals across all years
    /// </summary>
    private static (
        int TotalFacilities,
        decimal GrandTotalDiscountedCashflows,
        decimal TotalPortfolioLoss,
        decimal AverageLgd,
        decimal AverageRecoveryRate,
        decimal TotalMaxOutstanding,
        decimal AverageYearsFromNplToClosureDate,
        decimal MinYearsFromNplToClosureDate,
        decimal MaxYearsFromNplToClosureDate,
        int FacilitiesWithZeroYearsToClosureFromNpl
    ) CalculateOverallTotals(List<YearLgdClassification> yearClassifications)
    {
        if (yearClassifications.Count == 0)
        {
            return (0, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0);
        }

        // Get all facilities across all years and segments
        var allFacilities = yearClassifications
            .SelectMany(year => year.LgdClassifications)
            .SelectMany(segment => segment.FacilitySummaries)
            .ToList();

        int totalFacilities = allFacilities.Count;
        decimal grandTotalDiscountedCashflows = allFacilities.Sum(f => f.TotalDiscountedCashflows ?? 0m); // Handle nulls for VC Closed
        decimal totalPortfolioLoss = allFacilities.Sum(f => f.Loss ?? 0m); // Handle nulls for VC Closed
        decimal averageLgd = allFacilities.Count > 0 ? allFacilities.Average(f => f.Lgd) : 0m;
        decimal averageRecoveryRate = allFacilities.Count > 0 ? allFacilities.Average(f => f.RecoveryRate) : 0m;
        decimal totalMaxOutstanding = allFacilities.Sum(f => f.MaxTotalOutstandingAsAtFirstNplDate ?? 0m); // Handle nulls for VC Closed

        var yearsFromNplToClosureValues = allFacilities.Select(f => f.YearsFromNplToClosureDate).ToList();
        decimal averageYearsFromNplToClosureDate = yearsFromNplToClosureValues.Count > 0
            ? yearsFromNplToClosureValues.Average()
            : 0m;
        decimal minYearsFromNplToClosureDate = yearsFromNplToClosureValues.Count > 0
            ? yearsFromNplToClosureValues.Min()
            : 0m;
        decimal maxYearsFromNplToClosureDate = yearsFromNplToClosureValues.Count > 0
            ? yearsFromNplToClosureValues.Max()
            : 0m;
        int facilitiesWithZeroYearsToClosureFromNpl = yearsFromNplToClosureValues.Count(y => y == 0m);

        return (
            totalFacilities,
            grandTotalDiscountedCashflows,
            totalPortfolioLoss,
            averageLgd,
            averageRecoveryRate,
            totalMaxOutstanding,
            averageYearsFromNplToClosureDate,
            minYearsFromNplToClosureDate,
            maxYearsFromNplToClosureDate,
            facilitiesWithZeroYearsToClosureFromNpl
        );
    }
}

/// <summary>
/// Helper class to combine LGD details with file information for hierarchical processing
/// </summary>
public sealed record LgdDetailsWithFileInfo(
    LgdDetails LgdDetails,
    int Year,
    string Period,
    string FileName);