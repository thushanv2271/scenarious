using Application.DTOs.LGDCalculation;
using Microsoft.Extensions.Logging;
using Infrastructure.LgdCalculationSteps.Helpers.Step3;
using SharedKernel;
using System.Diagnostics;

namespace Infrastructure.LgdCalculationSteps.Steps;

/// <summary>
/// Step 3 of LGD Calculation: Yearly LGD average calculation that computes LGD averages and changes by financial year
/// from Step 2 hierarchical results
/// </summary>
internal sealed class Step3YearlyLgdAverageCalculation
{
    private readonly ILogger<Step3YearlyLgdAverageCalculation> _logger;

    public Step3YearlyLgdAverageCalculation(ILogger<Step3YearlyLgdAverageCalculation> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes Step 3: Compute yearly LGD averages and changes from Step 2 hierarchical results
    /// </summary>
    /// <param name="step2Result">Step 2 hierarchical calculation result containing the source data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing yearly LGD average calculation result</returns>
    public Task<Result<Step3YearlyLgdAverageResult>> ExecuteAsync(
        HierarchicalStep2LgdCalculationResult step2Result,
        CancellationToken cancellationToken = default)
    {
        var totalExecutionStopwatch = Stopwatch.StartNew();
        _logger.LogInformation("=== Step 3 LGD Yearly Average Calculation Started ===");

        try
        {
            // Validate input
            if (step2Result is null)
            {
                _logger.LogWarning("Step 2 result is null. Cannot proceed with Step 3 calculation.");
                return Task.FromResult(Result.Failure<Step3YearlyLgdAverageResult>(Error.Validation(
                    "LGDCalculation.Step3.InvalidInput",
                    "Step 2 result cannot be null. Please provide valid hierarchical calculation results from Step 2.")));
            }

            // ==== STEP 1: VALIDATE STEP 2 DATA ====
            var validationStopwatch = Stopwatch.StartNew();

            if (step2Result.YearClassifications.Count == 0)
            {
                _logger.LogWarning("Step 2 result contains no year classifications. Cannot compute yearly LGD averages.");
                return Task.FromResult(Result.Failure<Step3YearlyLgdAverageResult>(Error.NotFound(
                    "LGDCalculation.Step3.NoDataFound",
                    "No year classifications found in Step 2 results. Please ensure Step 2 has been executed successfully.")));
            }

            validationStopwatch.Stop();
            _logger.LogDebug("1. Data Validation: {ElapsedMs}ms ({ElapsedSec:F2}s) - {YearCount} years, {FacilityCount} facilities validated",
                validationStopwatch.ElapsedMilliseconds, validationStopwatch.Elapsed.TotalSeconds,
                step2Result.YearClassifications.Count, step2Result.TotalFacilities);

            // ==== STEP 2: COMPUTE YEARLY LGD AVERAGES ====
            var extractionStopwatch = Stopwatch.StartNew();
            _logger.LogDebug("2. Starting yearly LGD average calculation...");

            // Extract min/max years from the Step 2 result
            int minYears = Convert.ToInt32(step2Result.MinYearsFromNplToClosureDate);
            int maxYears = Convert.ToInt32(step2Result.MaxYearsFromNplToClosureDate);

            // Delegate the actual average LGD calculation to the helper
            Step3YearlyLgdAverageResult yearlyLgdResult = AverageLgdByFinancialYearCalculator.Calculate(step2Result, minYears, maxYears);

            extractionStopwatch.Stop();
            _logger.LogDebug("2. Yearly LGD Average Calculation: {ElapsedMs}ms ({ElapsedSec:F2}s) - Min: {MinYears}, Max: {MaxYears}",
                extractionStopwatch.ElapsedMilliseconds, extractionStopwatch.Elapsed.TotalSeconds, minYears, maxYears);

            totalExecutionStopwatch.Stop();
            _logger.LogInformation("=== Step 3 LGD Yearly Average Calculation Completed in {ElapsedMs}ms ({ElapsedSec:F2} seconds) ===",
                totalExecutionStopwatch.ElapsedMilliseconds, totalExecutionStopwatch.Elapsed.TotalSeconds);

            _logger.LogInformation("Yearly LGD Average Summary: {FacilityCount} facilities, {YearCount} years, Min Years: {MinYears}, Max Years: {MaxYears}",
                step2Result.TotalFacilities, step2Result.YearClassifications.Count, yearlyLgdResult.MinYearsFromNplToClosureDate, yearlyLgdResult.MaxYearsFromNplToClosureDate);

            return Task.FromResult(Result.Success(yearlyLgdResult));
        }
        catch (Exception ex)
        {
            totalExecutionStopwatch.Stop();
            _logger.LogError(ex, "Error occurred during LGD yearly average calculation Step 3 execution");
            return Task.FromResult(Result.Failure<Step3YearlyLgdAverageResult>(Error.Failure(
                "LGDCalculation.Step3.ExecutionError",
                $"An error occurred during LGD yearly average calculation Step 3: {ex.Message}")));
        }
    }
}