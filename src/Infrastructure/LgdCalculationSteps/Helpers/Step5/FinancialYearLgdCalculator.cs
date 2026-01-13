using Application.DTOs.LGDCalculation;

namespace Infrastructure.LgdCalculationSteps.Helpers.Step5;

/// <summary>
/// Calculator for Step 5 financial year-based LGD analysis
/// </summary>
internal static class FinancialYearLgdCalculator
{
    /// <summary>
    /// Calculates financial year-based LGD results from Step 2 hierarchical data
    /// </summary>
    /// <param name="step2Result">Step 2 hierarchical calculation result</param>
    /// <param name="financialYearEnds">List of financial year end dates</param>
    /// <returns>Step 5 financial year-based LGD calculation result</returns>
    public static Step5FinancialYearLgdResult Calculate(
        HierarchicalStep2LgdCalculationResult step2Result,
        List<DateTime> financialYearEnds)
    {
        ArgumentNullException.ThrowIfNull(step2Result);

        if (financialYearEnds is null || financialYearEnds.Count == 0)
        {
            throw new ArgumentException("Financial year ends cannot be null or empty", nameof(financialYearEnds));
        }

        // Sort financial year ends to ensure proper processing
        var sortedFinancialYearEnds = financialYearEnds.OrderBy(d => d).ToList();

        // Create financial year summaries
        var financialYearSummaries = new List<FinancialYearLgdSummary>();

        foreach (DateTime financialYearEnd in sortedFinancialYearEnds)
        {
            FinancialYearLgdSummary financialYearSummary = CalculateFinancialYearSummary(
                step2Result,
                financialYearEnd);

            financialYearSummaries.Add(financialYearSummary);
        }

        // Get all available classifications across all financial years
        var allAvailableClassifications = financialYearSummaries
            .SelectMany(fy => fy.Classifications)
            .Select(c => c.ClassificationName)
            .Distinct()
            .OrderBy(name => name)
            .ToList();

        return new Step5FinancialYearLgdResult
        {
            FinancialYearSummaries = financialYearSummaries,
            AllAvailableClassifications = allAvailableClassifications,
            CalculationTimestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Calculates LGD summary for a specific financial year
    /// </summary>
    /// <param name="step2Result">Step 2 hierarchical calculation result</param>
    /// <param name="financialYearEnd">Financial year end date</param>
    /// <returns>Financial year LGD summary</returns>
    private static FinancialYearLgdSummary CalculateFinancialYearSummary(
        HierarchicalStep2LgdCalculationResult step2Result,
        DateTime financialYearEnd)
    {
        // Calculate financial year start date (assuming a 12-month financial year)
        DateTime financialYearStart = financialYearEnd.AddYears(-1).AddDays(1);

        // Filter Step 2 data to include years that fall within this financial year period
        var relevantYearClassifications = step2Result.YearClassifications
            .Where(yc => IsYearInFinancialYear(yc.Year, financialYearStart, financialYearEnd))
            .ToList();

        // Group and aggregate data by classification
        List<FinancialYearClassificationSummary> classifications = new();

        IEnumerable<IGrouping<string, SegmentLgdClassification>> classificationGroups = relevantYearClassifications
            .SelectMany(yc => yc.LgdClassifications)
            .GroupBy(c => c.LgdClassification);

        foreach (IGrouping<string, SegmentLgdClassification> classificationGroup in classificationGroups)
        {
            FinancialYearClassificationSummary classificationSummary = CalculateClassificationSummary(
                classificationGroup.ToList());

            classifications.Add(classificationSummary);
        }

        return new FinancialYearLgdSummary
        {
            FinancialYearEndDate = financialYearEnd,
            Classifications = classifications
        };
    }

    /// <summary>
    /// Calculates classification summary by aggregating facilities across multiple years
    /// </summary>
    /// <param name="classifications">List of classifications from different years</param>
    /// <param name="contributingYears">List of years that contributed to this summary</param>
    /// <returns>Financial year classification summary</returns>
    private static FinancialYearClassificationSummary CalculateClassificationSummary(
        List<SegmentLgdClassification> classifications)
    {
        if (classifications.Count == 0)
        {
            throw new ArgumentException("Classifications cannot be empty", nameof(classifications));
        }

        string classificationName = classifications[0].LgdClassification;

        // Aggregate all facilities across all years for this classification
        var allFacilities = classifications.SelectMany(c => c.FacilitySummaries).ToList();

        // Calculate only the required totals
        decimal totalLoss = allFacilities.Sum(f => f.Loss ?? 0);
        decimal totalOutstandingAsAtFirstNplDate = allFacilities.Sum(f => f.MaxTotalOutstandingAsAtFirstNplDate ?? 0);

        return new FinancialYearClassificationSummary
        {
            ClassificationName = classificationName,
            TotalOutstandingAsAtFirstNplDate = totalOutstandingAsAtFirstNplDate,
            TotalLoss = totalLoss
        };
    }

    /// <summary>
    /// Determines if a calendar year falls within a financial year period
    /// </summary>
    /// <param name="calendarYear">Calendar year to check</param>
    /// <param name="financialYearStart">Start of financial year period</param>
    /// <param name="financialYearEnd">End of financial year period</param>
    /// <returns>True if the calendar year falls within the financial year period</returns>
    private static bool IsYearInFinancialYear(int calendarYear, DateTime financialYearStart, DateTime financialYearEnd)
    {
        // Check if any part of the calendar year overlaps with the financial year period
        DateTime calendarYearStart = new(calendarYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime calendarYearEnd = new(calendarYear, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        return calendarYearStart <= financialYearEnd && calendarYearEnd >= financialYearStart;
    }
}