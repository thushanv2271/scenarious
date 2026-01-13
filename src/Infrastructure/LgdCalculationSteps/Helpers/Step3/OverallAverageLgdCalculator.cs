using Application.DTOs.LGDCalculation;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Infrastructure.LgdCalculationSteps.Helpers.Step3;

// Suppress conflicting analyzer style rules in this helper where mixed 'var'/explicit types are acceptable
#pragma warning disable IDE0007 // Use 'var' instead of explicit type
#pragma warning disable IDE0008 // Use explicit type instead of 'var'

/// <summary>
/// Helper that computes overall average LGD per classification per years-from-NPL-to-closure
/// across all financial years, following Single Responsibility Principle.
/// </summary>
internal static class OverallAverageLgdCalculator
{
    /// <summary>
    /// Calculates the overall average LGD per classification per years-from-NPL-to-closure
    /// by aggregating values across all years. Returns null when no valid data is found.
    /// Also calculates delta LGD compared to previous years-from-NPL period.
    /// </summary>
    /// <param name="yearResults">Year results from Step 3 calculation</param>
    /// <param name="minYears">Minimum years from NPL to closure</param>
    /// <param name="maxYears">Maximum years from NPL to closure</param>
    /// <returns>List of classification overall averages with LGD and delta calculations</returns>
    public static List<ClassificationOverallAverage> Calculate(
        List<YearLgdAverageResult> yearResults,
        int minYears,
        int maxYears)
    {
        ArgumentNullException.ThrowIfNull(yearResults);

        var classificationOverallAverages = new List<ClassificationOverallAverage>();

        // Get all unique classifications across all years
        var allClassifications = yearResults
            .SelectMany(yr => yr.Classifications)
            .Select(c => c.LgdClassification)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        foreach (string classification in allClassifications)
        {
            var yearsFromNplDataList = new List<YearsFromNplLgdData>();
            decimal? previousAvgLgd = null;

            // Process regular periods from minYears to maxYears
            for (int yearsFromNpl = minYears; yearsFromNpl <= maxYears; yearsFromNpl++)
            {
                // Collect all non-null LGD values for this classification and years-from-NPL across all years
                var lgdValues = yearResults
                    .SelectMany(yearResult => yearResult.Classifications)
                    .Where(c => c.LgdClassification == classification)
                    .Select(c => c.AverageLgdByYearsFromNplToClosureDate.GetValueOrDefault(yearsFromNpl, null))
                    .Where(value => value.HasValue) // Only include non-null values
                    .Select(value => value!.Value)
                    .ToList();

                decimal currentAvgLgd = 0m; // Default to 0% if no data
                if (lgdValues.Count > 0)
                {
                    currentAvgLgd = decimal.Round(lgdValues.Sum() / lgdValues.Count, 6);
                }

                // Calculate delta LGD (current - previous)
                decimal? deltaLgd = null;
                if (previousAvgLgd.HasValue)
                {
                    deltaLgd = decimal.Round(currentAvgLgd - previousAvgLgd.Value, 6);
                }

                yearsFromNplDataList.Add(new YearsFromNplLgdData
                {
                    YearsFromNplToClosureDate = yearsFromNpl,
                    AvgLgd = currentAvgLgd,
                    DeltaLgd = deltaLgd
                });

                // Update previous value for next iteration
                previousAvgLgd = currentAvgLgd;
            }

            // Add the extra period at maxYears + 1 with 0% LGD (complete write-off scenario)
            decimal finalDeltaLgd = previousAvgLgd.HasValue
                ? decimal.Round(0m - previousAvgLgd.Value, 6)
                : 0m;

            yearsFromNplDataList.Add(new YearsFromNplLgdData
            {
                YearsFromNplToClosureDate = maxYears + 1,
                AvgLgd = 0m, // 0% LGD = complete write-off
                DeltaLgd = finalDeltaLgd
            });

            classificationOverallAverages.Add(new ClassificationOverallAverage
            {
                Classification = classification,
                YearsFromNplData = yearsFromNplDataList
            });
        }

        return classificationOverallAverages;
    }
}