using Application.DTOs.LGDCalculation;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Infrastructure.LgdCalculationSteps.Helpers.Step3;

// Suppress conflicting analyzer style rules in this helper where mixed 'var'/explicit types are acceptable
#pragma warning disable IDE0007 // Use 'var' instead of explicit type
#pragma warning disable IDE0008 // Use explicit type instead of 'var'

/// <summary>
/// Helper that computes Average LGD of each financial year per LGD classification
/// grouped by yearsFromNplToClosureDate.
/// </summary>
internal static class AverageLgdByFinancialYearCalculator
{
    /// <summary>
    /// Calculates the average LGD by financial year from a hierarchical Step 2 result using provided min/max years.
    /// </summary>
    public static Step3YearlyLgdAverageResult Calculate(HierarchicalStep2LgdCalculationResult step2Result, int minYears, int maxYears)
    {
        ArgumentNullException.ThrowIfNull(step2Result);

        Step3YearlyLgdAverageResult result = new Step3YearlyLgdAverageResult
        {
            MinYearsFromNplToClosureDate = minYears,
            MaxYearsFromNplToClosureDate = maxYears,
            CalculationTimestamp = DateTime.UtcNow
        };

        var yearResults = new List<YearLgdAverageResult>();

        foreach (var yearClassification in step2Result.YearClassifications)
        {
            YearLgdAverageResult yearResult = new YearLgdAverageResult { Year = yearClassification.Year };

            foreach (var classification in yearClassification.LgdClassifications)
            {
                var classificationResult = new ClassificationLgdAverageResult
                {
                    LgdClassification = classification.LgdClassification
                };

                for (int y = minYears; y <= maxYears; y++)
                {
                    // collect LGD values for facilities in this classification with the given yearsFromNplToClosureDate
                    var values = classification.FacilitySummaries
                        .Where(f => f.YearsFromNplToClosureDate == y)
                        .Select(f => f.Lgd)
                        .ToList();

                    decimal? avg = null;
                    if (values.Count > 0)
                    {
                        avg = decimal.Round(values.Aggregate(0m, (acc, v) => acc + v) / values.Count, 6);
                    }

                    classificationResult.AverageLgdByYearsFromNplToClosureDate[y] = avg;
                }

                yearResult.Classifications.Add(classificationResult);
            }

            yearResults.Add(yearResult);
        }

        // Calculate overall averages across all years using the separate helper (SOLID principle)
        var overallAverages = OverallAverageLgdCalculator.Calculate(yearResults, minYears, maxYears);

        // Set the results
        result.YearResults.AddRange(yearResults);
        result.OverallAverageLgdByClassificationAndYears.AddRange(overallAverages);

        return result;
    }
}
