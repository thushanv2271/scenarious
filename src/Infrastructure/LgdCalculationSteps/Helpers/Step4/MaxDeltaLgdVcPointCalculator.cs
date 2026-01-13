using Application.DTOs.LGDCalculation;

namespace Infrastructure.LgdCalculationSteps.Helpers.Step4;

// Suppress conflicting analyzer style rules in this helper where mixed 'var'/explicit types are acceptable
#pragma warning disable IDE0007 // Use 'var' instead of explicit type
#pragma warning disable IDE0008 // Use explicit type instead of 'var'

/// <summary>
/// Helper that implements the "Maximum Delta LGD minus one" method for VC-point determination.
/// Following Single Responsibility Principle - this class is only responsible for this specific calculation method.
/// </summary>
internal static class MaxDeltaLgdVcPointCalculator
{
    /// <summary>
    /// Calculates VC-point using the "Maximum Delta LGD minus one" method:
    /// 1. Find the maximum positive change (Delta LGD) in loss given default over the years
    /// 2. Identify how many years from the NPL date to closure correspond to this maximum Delta LGD
    /// 3. Then subtract 1 from this number of years
    /// </summary>
    /// <param name="step3Result">Step 3 yearly LGD average result containing overall average data</param>
    /// <returns>VC-point determination result</returns>
    public static Step4VcPointDeterminationResult Calculate(Step3YearlyLgdAverageResult step3Result)
    {
        ArgumentNullException.ThrowIfNull(step3Result);

        Step4VcPointDeterminationResult result = new Step4VcPointDeterminationResult
        {
            CalculationTimestamp = DateTime.UtcNow,
            DeterminationMethod = "Maximum Delta LGD minus one"
        };

        var classificationResults = new List<ClassificationVcPointResult>();

        foreach (var classificationData in step3Result.OverallAverageLgdByClassificationAndYears)
        {
            var classificationResult = DetermineVcPointForClassification(classificationData);
            classificationResults.Add(classificationResult);
        }

        result = result with { ClassificationResults = classificationResults };

        return result;
    }

    /// <summary>
    /// Determines VC-point for a single classification using the max delta LGD method.
    /// Implements the Open/Closed Principle - can be extended for other determination methods without modification.
    /// </summary>
    /// <param name="classificationData">Classification data containing years from NPL data with delta LGD values</param>
    /// <returns>VC-point result for the classification</returns>
    private static ClassificationVcPointResult DetermineVcPointForClassification(ClassificationOverallAverage classificationData)
    {
        ArgumentNullException.ThrowIfNull(classificationData);

        var deltaLgdByYears = new Dictionary<int, decimal>();
        decimal maxDeltaLgd = decimal.MinValue; // Track the maximum positive delta
        int yearsAtMaxDelta = 0;
        bool hasValidData = false;

        // Process each year's data to find delta LGD values
        foreach (var yearData in classificationData.YearsFromNplData)
        {
            // Skip entries without delta (first entry typically has null delta)
            if (!yearData.DeltaLgd.HasValue)
            {
                continue;
            }

            decimal deltaLgd = yearData.DeltaLgd.Value;
            deltaLgdByYears[yearData.YearsFromNplToClosureDate] = deltaLgd;
            hasValidData = true;

            // Find maximum delta LGD (including negative values to find the highest increase)
            if (deltaLgd > maxDeltaLgd)
            {
                maxDeltaLgd = deltaLgd;
                yearsAtMaxDelta = yearData.YearsFromNplToClosureDate;
            }
        }

        // Determine VC-point: subtract 1 from the years where max delta occurred
        int vcPoint = Math.Max(0, yearsAtMaxDelta - 1); // Ensure VC-point is not negative

        string notes = hasValidData
            ? $"Found maximum delta LGD of {maxDeltaLgd:F6} at {yearsAtMaxDelta} years. VC-point = {yearsAtMaxDelta} - 1 = {vcPoint}."
            : "No valid delta LGD data found. VC-point set to 0.";

        return new ClassificationVcPointResult
        {
            Classification = classificationData.Classification,
            VcPoint = vcPoint,
            MaxDeltaLgd = maxDeltaLgd,
            YearsAtMaxDelta = yearsAtMaxDelta,
            CalculationDetails = new VcPointCalculationDetails
            {
                DeltaLgdByYears = deltaLgdByYears,
                HasValidData = hasValidData,
                Notes = notes
            }
        };
    }
}