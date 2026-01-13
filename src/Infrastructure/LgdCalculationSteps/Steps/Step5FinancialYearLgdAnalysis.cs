using Application.Abstractions.Data;
using Application.DTOs.LGDCalculation;
using Infrastructure.LgdCalculationSteps.Helpers.Step5;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;
using System.Diagnostics;
using System.Globalization;

namespace Infrastructure.LgdCalculationSteps.Steps;

/// <summary>
/// Step 5 of LGD Calculation: Financial year-based LGD analysis using Step 2 hierarchical data
/// </summary>
internal sealed class Step5FinancialYearLgdAnalysis
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<Step5FinancialYearLgdAnalysis> _logger;

    public Step5FinancialYearLgdAnalysis(
        IApplicationDbContext dbContext,
        ILogger<Step5FinancialYearLgdAnalysis> logger)
    {
        _dbContext = dbContext;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes Step 5: Financial year-based LGD analysis from Step 2 hierarchical data
    /// </summary>
    /// <param name="step2Result">Step 2 hierarchical calculation result containing the source data</param>
    /// <param name="financialYearEnds">List of financial year end dates for analysis</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing financial year-based LGD analysis</returns>
    public async Task<Result<Step5FinancialYearLgdResult>> ExecuteAsync(
        HierarchicalStep2LgdCalculationResult step2Result,
        List<DateTime> financialYearEnds,
        CancellationToken cancellationToken = default)
    {
        var totalExecutionStopwatch = Stopwatch.StartNew();
        _logger.LogInformation("=== Step 5 LGD Financial Year Analysis Started ===");

        try
        {
            // ==== STEP 1: VALIDATE INPUT PARAMETERS ====
            var validationStopwatch = Stopwatch.StartNew();

            if (step2Result is null)
            {
                _logger.LogWarning("Step 2 result is null. Cannot proceed with Step 5 financial year analysis.");
                return Result.Failure<Step5FinancialYearLgdResult>(Error.Validation(
                    "LGDCalculation.Step5.InvalidInput",
                    "Step 2 result cannot be null. Please provide valid hierarchical LGD results from Step 2."));
            }

            if (financialYearEnds is null || financialYearEnds.Count == 0)
            {
                _logger.LogWarning("Financial year ends list is null or empty. Cannot proceed with Step 5 financial year analysis.");
                return Result.Failure<Step5FinancialYearLgdResult>(Error.Validation(
                    "LGDCalculation.Step5.InvalidFinancialYearEnds",
                    "Financial year ends cannot be null or empty. Please provide at least one financial year end date."));
            }

            if (step2Result.YearClassifications.Count == 0)
            {
                _logger.LogWarning("Step 2 result contains no year classifications. Cannot proceed with Step 5 financial year analysis.");
                return Result.Failure<Step5FinancialYearLgdResult>(Error.Validation(
                    "LGDCalculation.Step5.NoYearClassifications",
                    "Step 2 result contains no year classifications. Please ensure Step 2 was executed with valid data."));
            }

            validationStopwatch.Stop();
            _logger.LogDebug("1. Input Validation: {ElapsedMs}ms ({ElapsedSec:F2}s) - Step 2 data: {YearCount} years, {FacilityCount} facilities; Financial year ends: {FinancialYearCount}",
                validationStopwatch.ElapsedMilliseconds, validationStopwatch.Elapsed.TotalSeconds,
                step2Result.YearClassifications.Count, step2Result.TotalFacilities, financialYearEnds.Count);

            // Sort financial year ends for consistent processing
            var sortedFinancialYearEnds = financialYearEnds.OrderBy(d => d).ToList();

            // ==== STEP 1.5: VALIDATE FINANCIAL YEAR ENDS AGAINST AVAILABLE DATA ====
            var dataValidationStopwatch = Stopwatch.StartNew();
            _logger.LogDebug("1.5. Validating financial year ends against available data...");

            Result<List<int>> availableYearsResult = await GetAvailableYearsFromDatabaseAsync(cancellationToken);
            if (availableYearsResult.IsFailure)
            {
                return Result.Failure<Step5FinancialYearLgdResult>(availableYearsResult.Error);
            }

            List<int> availableYears = availableYearsResult.Value;
            _logger.LogDebug("Available years in database: {AvailableYears}", string.Join(", ", availableYears.OrderBy(y => y)));

            // Check if all financial year ends have corresponding data
            List<DateTime> missingFinancialYearEnds = new();
            foreach (DateTime financialYearEnd in sortedFinancialYearEnds)
            {
                bool hasCorrespondingData = HasCorrespondingDataForFinancialYear(financialYearEnd, availableYears);
                if (!hasCorrespondingData)
                {
                    missingFinancialYearEnds.Add(financialYearEnd);
                }
            }

            if (missingFinancialYearEnds.Count > 0)
            {
                string missingYears = string.Join(", ", missingFinancialYearEnds.Select(d => d.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)));
                _logger.LogWarning("Financial year ends with no corresponding data: {MissingYears}", missingYears);
                return Result.Failure<Step5FinancialYearLgdResult>(Error.Validation(
                    "LGDCalculation.Step5.MissingFinancialYearData",
                    $"Financial year end(s) for {missingYears} are not available in the database. Please ensure Step 1 has been executed for all required years."));
            }

            dataValidationStopwatch.Stop();
            _logger.LogDebug("1.5. Financial Year Data Validation: {ElapsedMs}ms ({ElapsedSec:F2}s) - All {FinancialYearCount} financial year ends have corresponding data",
                dataValidationStopwatch.ElapsedMilliseconds, dataValidationStopwatch.Elapsed.TotalSeconds, sortedFinancialYearEnds.Count);

            _logger.LogInformation("Processing financial year analysis for {FinancialYearCount} financial years: {FinancialYearRange}",
                sortedFinancialYearEnds.Count,
                $"{sortedFinancialYearEnds[0]:yyyy-MM-dd} to {sortedFinancialYearEnds[^1]:yyyy-MM-dd}");

            // Log Step 2 source data summary
            _logger.LogInformation("Source Step 2 data: {YearCount} calendar years ({YearRange}), {ClassificationCount} unique classifications, {FacilityCount} total facilities, Grand Total: {GrandTotal:C}",
                step2Result.YearClassifications.Count,
                step2Result.YearClassifications.Count > 0
                    ? $"{step2Result.YearClassifications.Min(yc => yc.Year)} to {step2Result.YearClassifications.Max(yc => yc.Year)}"
                    : "N/A",
                step2Result.YearClassifications.SelectMany(yc => yc.LgdClassifications).Select(c => c.LgdClassification).Distinct().Count(),
                step2Result.TotalFacilities,
                step2Result.GrandTotalDiscountedCashflows);

            // ==== STEP 2: CALCULATE FINANCIAL YEAR-BASED LGD ANALYSIS ====
            var calculationStopwatch = Stopwatch.StartNew();
            _logger.LogDebug("2. Starting financial year-based LGD analysis calculation...");

            Step5FinancialYearLgdResult result = FinancialYearLgdCalculator.Calculate(
                step2Result,
                sortedFinancialYearEnds);

            calculationStopwatch.Stop();
            _logger.LogDebug("2. Calculate Financial Year-based LGD Analysis: {ElapsedMs}ms ({ElapsedSec:F2}s) - {FinancialYearCount} financial years processed",
                calculationStopwatch.ElapsedMilliseconds, calculationStopwatch.Elapsed.TotalSeconds,
                result.FinancialYearSummaries.Count);

            totalExecutionStopwatch.Stop();
            _logger.LogInformation("=== Step 5 LGD Financial Year Analysis Completed in {ElapsedMs}ms ({ElapsedSec:F2} seconds) ===",
                totalExecutionStopwatch.ElapsedMilliseconds, totalExecutionStopwatch.Elapsed.TotalSeconds);

            _logger.LogInformation("Financial Year Analysis Summary: {FinancialYearCount} financial years, {ClassificationCount} classifications available",
                result.FinancialYearSummaries.Count, result.AllAvailableClassifications.Count);

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

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during Step 5 financial year LGD analysis execution");
            return Result.Failure<Step5FinancialYearLgdResult>(Error.Failure(
                "LGDCalculation.Step5.ExecutionError",
                $"An error occurred during Step 5 financial year LGD analysis: {ex.Message}"));
        }
    }

    /// <summary>
    /// Gets available years from the database by checking LgdFileDetails and VCLgdFileDetails
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available years</returns>
    private async Task<Result<List<int>>> GetAvailableYearsFromDatabaseAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Get years from LgdFileDetails
            List<int> lgdYears = await _dbContext.LgdFileDetails
                .AsNoTracking()
                .Select(fd => fd.Year)
                .Distinct()
                .ToListAsync(cancellationToken);

            // Get years from VCLgdFileDetails
            List<int> vcLgdYears = await _dbContext.VCLgdFileDetails
                .AsNoTracking()
                .Select(fd => fd.Year)
                .Distinct()
                .ToListAsync(cancellationToken);

            // Combine and deduplicate years
            var allAvailableYears = lgdYears.Union(vcLgdYears).OrderBy(y => y).ToList();

            return Result.Success(allAvailableYears);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving available years from database");
            return Result.Failure<List<int>>(Error.Failure(
                "LGDCalculation.Step5.DatabaseYearsRetrievalError",
                $"Failed to retrieve available years from database: {ex.Message}"));
        }
    }

    /// <summary>
    /// Checks if a financial year has corresponding data in the available years
    /// </summary>
    /// <param name="financialYearEnd">Financial year end date</param>
    /// <param name="availableYears">List of years available in the database</param>
    /// <returns>True if the financial year has corresponding data</returns>
    private static bool HasCorrespondingDataForFinancialYear(DateTime financialYearEnd, List<int> availableYears)
    {
        // Calculate the financial year start date (assuming a 12-month financial year)
        DateTime financialYearStart = financialYearEnd.AddYears(-1).AddDays(1);

        // Check if any calendar year in the financial year period has data
        for (DateTime date = financialYearStart; date <= financialYearEnd; date = date.AddYears(1))
        {
            int calendarYear = date.Year;
            if (availableYears.Contains(calendarYear))
            {
                return true;
            }
        }

        return false;
    }
}