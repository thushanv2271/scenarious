using Application.DTOs.LGDCalculation;

namespace Infrastructure.LgdCalculationSteps.Helpers.Step6;

/// <summary>
/// Calculator for Step 6: Sum of two Step 5 financial year LGD analysis results
/// </summary>
internal static class Step6TwoPayloadSumCalculator
{
    /// <summary>
    /// Calculates the sum of two Step 5 financial year LGD results
    /// </summary>
    /// <param name="payload1">First Step 5 result</param>
    /// <param name="payload2">Second Step 5 result</param>
    /// <returns>Step 5 result containing the sum of both payloads</returns>
    public static Step5FinancialYearLgdResult CalculateSum(
        Step5FinancialYearLgdResult payload1,
        Step5FinancialYearLgdResult payload2)
    {
        ArgumentNullException.ThrowIfNull(payload1);
        ArgumentNullException.ThrowIfNull(payload2);

        // Get all unique financial year end dates from both payloads
        var allFinancialYearEnds = payload1.FinancialYearSummaries
            .Select(fy => fy.FinancialYearEndDate)
            .Concat(payload2.FinancialYearSummaries.Select(fy => fy.FinancialYearEndDate))
            .Distinct()
            .OrderBy(date => date)
            .ToList();

        // Get all unique classifications from both payloads
        var allClassifications = payload1.AllAvailableClassifications
            .Concat(payload2.AllAvailableClassifications)
            .Distinct()
            .OrderBy(name => name)
            .ToList();

        // Calculate summed financial year summaries
        List<FinancialYearLgdSummary> summedFinancialYearSummaries = new();

        foreach (DateTime financialYearEnd in allFinancialYearEnds)
        {
            FinancialYearLgdSummary summedSummary = CalculateSummedFinancialYearSummary(
                payload1, payload2, financialYearEnd);

            summedFinancialYearSummaries.Add(summedSummary);
        }

        // Calculate final LGD percentages by classification
        Dictionary<string, decimal> finalLgdPercentages = CalculateFinalLgdPercentages(summedFinancialYearSummaries, allClassifications);

        return new Step5FinancialYearLgdResult
        {
            FinancialYearSummaries = summedFinancialYearSummaries,
            AllAvailableClassifications = allClassifications,
            FinalLgdPercentagesByClassification = finalLgdPercentages,
            CalculationTimestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Calculates the summed financial year summary for a specific financial year end date
    /// </summary>
    /// <param name="payload1">First Step 5 result</param>
    /// <param name="payload2">Second Step 5 result</param>
    /// <param name="financialYearEnd">Financial year end date to calculate sum for</param>
    /// <returns>Summed financial year summary</returns>
    private static FinancialYearLgdSummary CalculateSummedFinancialYearSummary(
        Step5FinancialYearLgdResult payload1,
        Step5FinancialYearLgdResult payload2,
        DateTime financialYearEnd)
    {
        // Get financial year summaries for the specific end date from both payloads
        FinancialYearLgdSummary? summary1 = payload1.FinancialYearSummaries
            .FirstOrDefault(fy => fy.FinancialYearEndDate == financialYearEnd);

        FinancialYearLgdSummary? summary2 = payload2.FinancialYearSummaries
            .FirstOrDefault(fy => fy.FinancialYearEndDate == financialYearEnd);

        // Get all unique classifications for this financial year from both payloads
        var uniqueClassifications = new List<string>();

        if (summary1 is not null)
        {
            uniqueClassifications.AddRange(summary1.Classifications.Select(c => c.ClassificationName));
        }

        if (summary2 is not null)
        {
            uniqueClassifications.AddRange(summary2.Classifications.Select(c => c.ClassificationName));
        }

        uniqueClassifications = uniqueClassifications.Distinct().OrderBy(name => name).ToList();

        // Calculate summed classifications
        List<FinancialYearClassificationSummary> summedClassifications = new();

        foreach (string classificationName in uniqueClassifications)
        {
            FinancialYearClassificationSummary summedClassification = CalculateSummedClassification(
                summary1, summary2, classificationName);

            summedClassifications.Add(summedClassification);
        }

        return new FinancialYearLgdSummary
        {
            FinancialYearEndDate = financialYearEnd,
            Classifications = summedClassifications
        };
    }

    /// <summary>
    /// Calculates the summed classification summary for a specific classification name
    /// </summary>
    /// <param name="summary1">Financial year summary from first payload (can be null)</param>
    /// <param name="summary2">Financial year summary from second payload (can be null)</param>
    /// <param name="classificationName">Classification name to calculate sum for</param>
    /// <returns>Summed classification summary</returns>
    private static FinancialYearClassificationSummary CalculateSummedClassification(
        FinancialYearLgdSummary? summary1,
        FinancialYearLgdSummary? summary2,
        string classificationName)
    {
        // Get classification data from first payload (or default to 0 if not found)
        FinancialYearClassificationSummary? classification1 = summary1?.Classifications
            .FirstOrDefault(c => c.ClassificationName == classificationName);

        decimal totalOutstanding1 = classification1?.TotalOutstandingAsAtFirstNplDate ?? 0;
        decimal totalLoss1 = classification1?.TotalLoss ?? 0;

        // Get classification data from second payload (or default to 0 if not found)
        FinancialYearClassificationSummary? classification2 = summary2?.Classifications
            .FirstOrDefault(c => c.ClassificationName == classificationName);

        decimal totalOutstanding2 = classification2?.TotalOutstandingAsAtFirstNplDate ?? 0;
        decimal totalLoss2 = classification2?.TotalLoss ?? 0;

        // Calculate summed values
        decimal summedTotalOutstanding = totalOutstanding1 + totalOutstanding2;
        decimal summedTotalLoss = totalLoss1 + totalLoss2;

        return new FinancialYearClassificationSummary
        {
            ClassificationName = classificationName,
            TotalOutstandingAsAtFirstNplDate = summedTotalOutstanding,
            TotalLoss = summedTotalLoss
        };
    }

    /// <summary>
    /// Calculates final LGD percentages by classification across all financial years
    /// </summary>
    /// <param name="financialYearSummaries">List of financial year summaries</param>
    /// <param name="allClassifications">List of all available classifications</param>
    /// <returns>Dictionary of classification name to LGD percentage</returns>
    private static Dictionary<string, decimal> CalculateFinalLgdPercentages(
        List<FinancialYearLgdSummary> financialYearSummaries,
        List<string> allClassifications)
    {
        Dictionary<string, decimal> finalLgdPercentages = new();
        const decimal DefaultLgdPercentage = 45.0m;

        foreach (string classification in allClassifications)
        {
            // Sum total loss and total outstanding across all financial years for this classification
            decimal totalLossForClassification = 0;
            decimal totalOutstandingForClassification = 0;

            foreach (FinancialYearLgdSummary fysummaray in financialYearSummaries)
            {
                FinancialYearClassificationSummary? classificationSummary = fysummaray.Classifications
                    .FirstOrDefault(c => c.ClassificationName == classification);

                if (classificationSummary is not null)
                {
                    totalLossForClassification += classificationSummary.TotalLoss;
                    totalOutstandingForClassification += classificationSummary.TotalOutstandingAsAtFirstNplDate;
                }
            }

            // Calculate LGD percentage
            decimal lgdPercentage;
            if (totalOutstandingForClassification > 0)
            {
                lgdPercentage = totalLossForClassification / totalOutstandingForClassification * 100;
            }
            else
            {
                // Default to 45% if not enough data
                lgdPercentage = DefaultLgdPercentage;
            }

            finalLgdPercentages[classification] = Math.Round(lgdPercentage, 2);
        }

        return finalLgdPercentages;
    }
}