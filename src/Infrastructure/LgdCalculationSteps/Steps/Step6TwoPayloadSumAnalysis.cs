using Application.DTOs.LGDCalculation;
using Infrastructure.LgdCalculationSteps.Helpers.Step6;
using Microsoft.Extensions.Logging;
using SharedKernel;
using System.Diagnostics;
using System.Globalization;

namespace Infrastructure.LgdCalculationSteps.Steps;

/// <summary>
/// Step 6 of LGD Calculation: Sum of two Step 5 financial year LGD analysis results
/// </summary>
internal sealed class Step6TwoPayloadSumAnalysis
{
    private readonly ILogger<Step6TwoPayloadSumAnalysis> _logger;

    public Step6TwoPayloadSumAnalysis(ILogger<Step6TwoPayloadSumAnalysis> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes Step 6 analysis: calculates the sum of two Step 5 financial year LGD results
    /// </summary>
    /// <param name="payload1">First Step 5 financial year LGD result</param>
    /// <param name="payload2">Second Step 5 financial year LGD result</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the summed Step 5 financial year LGD result</returns>
    public Task<Result<Step5FinancialYearLgdResult>> ExecuteAsync(
        Step5FinancialYearLgdResult payload1,
        Step5FinancialYearLgdResult payload2,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var totalExecutionStopwatch = Stopwatch.StartNew();
            _logger.LogInformation("=== Starting Step 6 LGD Two Payload Sum Analysis ===");

            // ==== STEP 1: VALIDATE INPUT PAYLOADS ====
            _logger.LogDebug("1. Validating input payloads...");

            ArgumentNullException.ThrowIfNull(payload1);
            ArgumentNullException.ThrowIfNull(payload2);

            if (payload1.FinancialYearSummaries is null || payload1.FinancialYearSummaries.Count == 0)
            {
                return Task.FromResult(Result.Failure<Step5FinancialYearLgdResult>(Error.Validation(
                    "LGDCalculation.Step6.InvalidPayload1",
                    "First payload must contain financial year summaries")));
            }

            if (payload2.FinancialYearSummaries is null || payload2.FinancialYearSummaries.Count == 0)
            {
                return Task.FromResult(Result.Failure<Step5FinancialYearLgdResult>(Error.Validation(
                    "LGDCalculation.Step6.InvalidPayload2",
                    "Second payload must contain financial year summaries")));
            }

            _logger.LogDebug("1. Input validation completed - Payload1: {Payload1Count} financial years, Payload2: {Payload2Count} financial years",
                payload1.FinancialYearSummaries.Count, payload2.FinancialYearSummaries.Count);

            // ==== STEP 2: CALCULATE SUM OF TWO PAYLOADS ====
            var calculationStopwatch = Stopwatch.StartNew();
            _logger.LogDebug("2. Starting two payload sum calculation...");

            Step5FinancialYearLgdResult result = Step6TwoPayloadSumCalculator.CalculateSum(
                payload1,
                payload2);

            calculationStopwatch.Stop();
            _logger.LogDebug("2. Calculate Two Payload Sum: {ElapsedMs}ms ({ElapsedSec:F2}s) - {FinancialYearCount} financial years processed",
                calculationStopwatch.ElapsedMilliseconds, calculationStopwatch.Elapsed.TotalSeconds,
                result.FinancialYearSummaries.Count);

            totalExecutionStopwatch.Stop();
            _logger.LogInformation("=== Step 6 LGD Two Payload Sum Analysis Completed in {ElapsedMs}ms ({ElapsedSec:F2} seconds) ===",
                totalExecutionStopwatch.ElapsedMilliseconds, totalExecutionStopwatch.Elapsed.TotalSeconds);

            _logger.LogInformation("Two Payload Sum Analysis Summary: {FinancialYearCount} financial years, {ClassificationCount} classifications available",
                result.FinancialYearSummaries.Count, result.AllAvailableClassifications.Count);

            // Log final LGD percentages by classification
            foreach (KeyValuePair<string, decimal> lgdPercentage in result.FinalLgdPercentagesByClassification)
            {
                _logger.LogInformation("Final LGD Percentage - {Classification}: {LgdPercentage:F2}%",
                    lgdPercentage.Key, lgdPercentage.Value);
            }

            // Log financial year-by-financial year summary
            foreach (FinancialYearLgdSummary fyResult in result.FinancialYearSummaries)
            {
                decimal totalLoss = fyResult.Classifications.Sum(c => c.TotalLoss);
                decimal totalOutstanding = fyResult.Classifications.Sum(c => c.TotalOutstandingAsAtFirstNplDate);
                IEnumerable<string> classificationNames = fyResult.Classifications.Select(c => c.ClassificationName).OrderBy(n => n);

                _logger.LogInformation("Financial Year {Date}: {ClassificationCount} classifications ({Classifications}), Total Loss: {TotalLoss:C}, Total Outstanding: {TotalOutstanding:C}",
                    fyResult.FinancialYearEndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    fyResult.Classifications.Count,
                    string.Join(", ", classificationNames),
                    totalLoss,
                    totalOutstanding);
            }

            return Task.FromResult(Result.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during Step 6 two payload sum analysis execution");
            return Task.FromResult(Result.Failure<Step5FinancialYearLgdResult>(Error.Failure(
                "LGDCalculation.Step6.ExecutionError",
                $"An error occurred during Step 6 two payload sum analysis: {ex.Message}")));
        }
    }
}