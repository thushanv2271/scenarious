using Application.DTOs.LGDCalculation;
using Microsoft.Extensions.Logging;
using Infrastructure.LgdCalculationSteps.Helpers.Step4;
using SharedKernel;
using System.Diagnostics;

namespace Infrastructure.LgdCalculationSteps.Steps;

/// <summary>
/// Step 4 of LGD Calculation: VC-point determination that identifies optimal conversion point
/// from Step 3 yearly LGD average results
/// </summary>
internal sealed class Step4VcPointDetermination
{
    private readonly ILogger<Step4VcPointDetermination> _logger;

    public Step4VcPointDetermination(ILogger<Step4VcPointDetermination> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes Step 4: Determine VC-point from Step 3 yearly LGD average results
    /// </summary>
    /// <param name="step3Result">Step 3 yearly LGD average calculation result containing the source data</param>
    /// <param name="method">The method to use for VC-point determination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing VC-point determination result</returns>
    public Task<Result<Step4VcPointDeterminationResult>> ExecuteAsync(
        Step3YearlyLgdAverageResult step3Result,
        VcPointDeterminationMethod method = VcPointDeterminationMethod.MaxDeltaLgdMinusOne,
        CancellationToken cancellationToken = default)
    {
        var totalExecutionStopwatch = Stopwatch.StartNew();
        _logger.LogInformation("=== Step 4 LGD VC-Point Determination Started ===");

        try
        {
            // Validate input
            if (step3Result is null)
            {
                _logger.LogWarning("Step 3 result is null. Cannot proceed with Step 4 VC-point determination.");
                return Task.FromResult(Result.Failure<Step4VcPointDeterminationResult>(Error.Validation(
                    "LGDCalculation.Step4.InvalidInput",
                    "Step 3 result cannot be null. Please provide valid yearly LGD average results from Step 3.")));
            }

            // ==== STEP 1: VALIDATE STEP 3 DATA ====
            var validationStopwatch = Stopwatch.StartNew();

            if (step3Result.OverallAverageLgdByClassificationAndYears.Count == 0)
            {
                _logger.LogWarning("Step 3 result contains no overall average data. Cannot determine VC-point.");
                return Task.FromResult(Result.Failure<Step4VcPointDeterminationResult>(Error.NotFound(
                    "LGDCalculation.Step4.NoDataFound",
                    "No overall average LGD data found in Step 3 results. Please ensure Step 3 has been executed successfully.")));
            }

            validationStopwatch.Stop();
            _logger.LogDebug("1. Data Validation: {ElapsedMs}ms ({ElapsedSec:F2}s) - {ClassificationCount} classifications validated",
                validationStopwatch.ElapsedMilliseconds, validationStopwatch.Elapsed.TotalSeconds,
                step3Result.OverallAverageLgdByClassificationAndYears.Count);

            // ==== STEP 2: DETERMINE VC-POINT ====
            var determinationStopwatch = Stopwatch.StartNew();
            _logger.LogDebug("2. Starting VC-point determination using method: {Method}...", method);

            // Delegate to appropriate determination strategy based on method
            Step4VcPointDeterminationResult vcPointResult = method switch
            {
                VcPointDeterminationMethod.MaxDeltaLgdMinusOne => MaxDeltaLgdVcPointCalculator.Calculate(step3Result),
                _ => throw new ArgumentException($"Unsupported VC-point determination method: {method}", nameof(method))
            };

            determinationStopwatch.Stop();
            _logger.LogDebug("2. VC-Point Determination: {ElapsedMs}ms ({ElapsedSec:F2}s) - {ClassificationCount} classifications processed",
                determinationStopwatch.ElapsedMilliseconds, determinationStopwatch.Elapsed.TotalSeconds,
                vcPointResult.ClassificationResults.Count);

            totalExecutionStopwatch.Stop();
            _logger.LogInformation("=== Step 4 LGD VC-Point Determination Completed in {ElapsedMs}ms ({ElapsedSec:F2} seconds) ===",
                totalExecutionStopwatch.ElapsedMilliseconds, totalExecutionStopwatch.Elapsed.TotalSeconds);

            _logger.LogInformation("VC-Point Determination Summary: {ClassificationCount} classifications processed using {Method}",
                vcPointResult.ClassificationResults.Count, vcPointResult.DeterminationMethod);

            return Task.FromResult(Result.Success(vcPointResult));
        }
        catch (Exception ex)
        {
            totalExecutionStopwatch.Stop();
            _logger.LogError(ex, "Error occurred during LGD VC-point determination Step 4 execution");
            return Task.FromResult(Result.Failure<Step4VcPointDeterminationResult>(Error.Failure(
                "LGDCalculation.Step4.ExecutionError",
                $"An error occurred during LGD VC-point determination Step 4: {ex.Message}")));
        }
    }
}