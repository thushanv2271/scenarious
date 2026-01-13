using Application.Abstractions.Calculations;
using Application.Abstractions.Configuration;
using Application.Abstractions.Data;
using Application.DTOs.LGDCalculation;
using Application.Models;
using Infrastructure.LgdCalculationSteps.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Infrastructure.Services;

/// <summary>
/// Implementation of the LGD Calculation service
/// </summary>
internal sealed class LgdCalculationService : ILgdCalculationService
{
    private readonly IAppConfiguration _appConfiguration;
    private readonly IApplicationDbContext _dbContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LgdCalculationService> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public LgdCalculationService(
        IAppConfiguration appConfiguration,
        IApplicationDbContext dbContext,
        IServiceProvider serviceProvider,
        ILogger<LgdCalculationService> logger,
        ILoggerFactory loggerFactory)
    {
        _appConfiguration = appConfiguration;
        _dbContext = dbContext;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Executes step 1 of LGD calculation
    /// </summary>
    /// <param name="createdBy">User who initiated the calculation</param>
    /// <param name="calculationType">Type of calculation to perform (LGD or VC_LGD)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    public async Task<Result> ExecuteStep1Async(string createdBy, LgdCalculationType calculationType = LgdCalculationType.LGD, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting LGD calculation Step 1 service execution for {CalculationType}", calculationType);

        try
        {
            ILogger<Step1LgdFileExtractionAndCalculation> stepLogger =
                _loggerFactory.CreateLogger<Step1LgdFileExtractionAndCalculation>();

            Step1LgdFileExtractionAndCalculation step1 = new(
                _appConfiguration,
                _dbContext,
                _serviceProvider,
                stepLogger);

            Result result = await step1.ExecuteAsync(createdBy, calculationType, null, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during LGD calculation Step 1 execution for {CalculationType}", calculationType);

            return Result.Failure(Error.Failure(
                "LGDCalculation.ExecutionError",
                $"An error occurred during LGD calculation Step 1 for {calculationType}: {ex.Message}"));
        }
    }

    /// <summary>
    /// Executes step 2 of LGD calculation with hierarchical structure: Year > LGD Classification (Segment) > Facility
    /// </summary>
    /// <param name="calculationType">Type of calculation to perform (LGD or VC_LGD)</param>
    /// <param name="vcPoint">Optional VC Point threshold value in years. Only used for VC_LGD calculations.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing hierarchical facility summaries organized by year, segment, and facility</returns>
    public async Task<Result<HierarchicalStep2LgdCalculationResult>> ExecuteStep2Async(LgdCalculationType calculationType = LgdCalculationType.LGD, decimal? vcPoint = null, Dictionary<string, decimal>? vcPointsByClassification = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting {CalculationType} calculation Step 2 hierarchical service execution", calculationType);

        try
        {
            ILogger<Step2LgdDiscountedCashflowSummary> stepLogger =
                _loggerFactory.CreateLogger<Step2LgdDiscountedCashflowSummary>();

            Step2LgdDiscountedCashflowSummary step2 = new(
                _dbContext,
                stepLogger);

            Result<HierarchicalStep2LgdCalculationResult> result = await step2.ExecuteAsync(calculationType, vcPoint, vcPointsByClassification, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during {CalculationType} calculation Step 2 hierarchical execution", calculationType);

            return Result.Failure<HierarchicalStep2LgdCalculationResult>(Error.Failure(
                $"{calculationType}Calculation.Step2.HierarchicalExecutionError",
                $"An error occurred during {calculationType} calculation Step 2 hierarchical execution: {ex.Message}"));
        }
    }

    /// <summary>
    /// Executes step 3 of LGD calculation: Yearly LGD average calculation that computes LGD averages and changes by financial year
    /// </summary>
    /// <param name="step2Result">Step 2 hierarchical calculation result containing the source data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing yearly LGD average calculation result</returns>
    public async Task<Result<Step3YearlyLgdAverageResult>> ExecuteStep3Async(HierarchicalStep2LgdCalculationResult step2Result, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting LGD calculation Step 3 Yearly LGD Average service execution");

        try
        {
            ILogger<Step3YearlyLgdAverageCalculation> stepLogger =
                _loggerFactory.CreateLogger<Step3YearlyLgdAverageCalculation>();

            Step3YearlyLgdAverageCalculation step3 = new(stepLogger);

            Result<Step3YearlyLgdAverageResult> result = await step3.ExecuteAsync(step2Result, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during LGD calculation Step 3 Yearly LGD Average execution");

            return Result.Failure<Step3YearlyLgdAverageResult>(Error.Failure(
                "LGDCalculation.Step3.YearlyLgdAverageExecutionError",
                $"An error occurred during LGD calculation Step 3 Yearly LGD Average execution: {ex.Message}"));
        }
    }

    /// <summary>
    /// Executes step 4 of LGD calculation: VC-point determination that identifies optimal conversion point
    /// </summary>
    /// <param name="step3Result">Step 3 yearly LGD average calculation result containing the source data</param>
    /// <param name="method">The method to use for VC-point determination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing VC-point determination result</returns>
    public async Task<Result<Step4VcPointDeterminationResult>> ExecuteStep4Async(Step3YearlyLgdAverageResult step3Result, VcPointDeterminationMethod method = VcPointDeterminationMethod.MaxDeltaLgdMinusOne, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting LGD calculation Step 4 VC-point determination service execution");

        try
        {
            ILogger<Step4VcPointDetermination> stepLogger =
                _loggerFactory.CreateLogger<Step4VcPointDetermination>();

            Step4VcPointDetermination step4 = new(stepLogger);

            Result<Step4VcPointDeterminationResult> result = await step4.ExecuteAsync(step3Result, method, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during LGD calculation Step 4 VC-point determination execution");

            return Result.Failure<Step4VcPointDeterminationResult>(Error.Failure(
                "LGDCalculation.Step4.VcPointDeterminationExecutionError",
                $"An error occurred during LGD calculation Step 4 VC-point determination execution: {ex.Message}"));
        }
    }

    /// <summary>
    /// Executes step 5 of LGD calculation: Financial year-based LGD analysis using Step 2 hierarchical data
    /// </summary>
    /// <param name="step2Result">Step 2 hierarchical calculation result containing the source data</param>
    /// <param name="financialYearEnds">List of financial year end dates for analysis</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing financial year-based LGD analysis</returns>
    public async Task<Result<Step5FinancialYearLgdResult>> ExecuteStep5Async(HierarchicalStep2LgdCalculationResult step2Result, List<DateTime> financialYearEnds, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting LGD calculation Step 5 financial year analysis service execution");

        try
        {
            ILogger<Step5FinancialYearLgdAnalysis> stepLogger =
                _loggerFactory.CreateLogger<Step5FinancialYearLgdAnalysis>();

            Step5FinancialYearLgdAnalysis step5 = new(_dbContext, stepLogger);

            Result<Step5FinancialYearLgdResult> result = await step5.ExecuteAsync(step2Result, financialYearEnds, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during LGD calculation Step 5 financial year analysis execution");

            return Result.Failure<Step5FinancialYearLgdResult>(Error.Failure(
                "LGDCalculation.Step5.FinancialYearAnalysisExecutionError",
                $"An error occurred during LGD calculation Step 5 financial year analysis execution: {ex.Message}"));
        }
    }

    /// <summary>
    /// Executes step 6 of LGD calculation: Sum of two Step 5 financial year LGD analysis results
    /// </summary>
    /// <param name="payload1">First Step 5 financial year LGD result</param>
    /// <param name="payload2">Second Step 5 financial year LGD result</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the summed Step 5 financial year LGD result</returns>
    public async Task<Result<Step5FinancialYearLgdResult>> ExecuteStep6Async(Step5FinancialYearLgdResult payload1, Step5FinancialYearLgdResult payload2, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting LGD calculation Step 6 two payload sum analysis service execution");

        try
        {
            ILogger<Step6TwoPayloadSumAnalysis> stepLogger =
                _loggerFactory.CreateLogger<Step6TwoPayloadSumAnalysis>();

            Step6TwoPayloadSumAnalysis step6 = new(stepLogger);

            Result<Step5FinancialYearLgdResult> result = await step6.ExecuteAsync(payload1, payload2, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during LGD calculation Step 6 two payload sum analysis execution");

            return Result.Failure<Step5FinancialYearLgdResult>(Error.Failure(
                "LGDCalculation.Step6.TwoPayloadSumAnalysisExecutionError",
                $"An error occurred during LGD calculation Step 6 two payload sum analysis execution: {ex.Message}"));
        }
    }
}