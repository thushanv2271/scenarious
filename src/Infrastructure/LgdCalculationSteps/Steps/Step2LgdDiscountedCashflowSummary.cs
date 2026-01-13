using Application.Abstractions.Data;
using Application.DTOs.LGDCalculation;
using Application.Models;
using Domain.LGDCalculation;
using Infrastructure.LgdCalculationSteps.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;
using System.Diagnostics;

namespace Infrastructure.LgdCalculationSteps.Steps;

/// <summary>
/// Step 2 of LGD Calculation: Compute hierarchical sum of discounted cashflows organized by Year > Segment > Facility
/// </summary>
internal sealed class Step2LgdDiscountedCashflowSummary
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<Step2LgdDiscountedCashflowSummary> _logger;

    public Step2LgdDiscountedCashflowSummary(
        IApplicationDbContext dbContext,
        ILogger<Step2LgdDiscountedCashflowSummary> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Executes Step 2: Calculate hierarchical LGD results organized by Year > LGD Classification (Segment) > Facility
    /// </summary>
    /// <param name="calculationType">Type of calculation to perform (LGD or VC_LGD)</param>
    /// <param name="vcPoint">Legacy VC Point threshold value in years. Only used for VC_LGD calculations for backward compatibility.</param>
    /// <param name="vcPointsByClassification">VC Point threshold values by classification. Only used for VC_LGD calculations. Takes precedence over vcPoint parameter.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing hierarchical facility summaries organized by year, segment, and facility</returns>
    public async Task<Result<HierarchicalStep2LgdCalculationResult>> ExecuteAsync(LgdCalculationType calculationType = LgdCalculationType.LGD, decimal? vcPoint = null, Dictionary<string, decimal>? vcPointsByClassification = null, CancellationToken cancellationToken = default)
    {
        var totalExecutionStopwatch = Stopwatch.StartNew();
        _logger.LogInformation("=== Step 2 {CalculationType} Hierarchical Calculation Started ===", calculationType);

        try
        {
            // ==== STEP 1: LOAD LGD DETAILS WITH FILE INFO FROM DATABASE ====
            var loadDataStopwatch = Stopwatch.StartNew();

            List<LgdDetailsWithFileInfo> lgdDetailsWithFileInfo = calculationType == LgdCalculationType.LGD
                ? await _dbContext.LgdDetails
                    .AsNoTracking()
                    .Include(detail => detail.LgdFileDetails)
                    .Where(detail => !string.IsNullOrWhiteSpace(detail.FacilityNumber))
                    .Select(detail => new LgdDetailsWithFileInfo(
                        detail,
                        detail.LgdFileDetails.Year,
                        detail.LgdFileDetails.Period,
                        detail.LgdFileDetails.FileName))
                    .ToListAsync(cancellationToken)
                : await _dbContext.VCLgdDetails
                    .AsNoTracking()
                    .Include(detail => detail.VCLgdFileDetails)
                    .Where(detail => !string.IsNullOrWhiteSpace(detail.FacilityNumber))
                    .Select(detail => new LgdDetailsWithFileInfo(
                        // Create an LgdDetails object from VCLgdDetails for compatibility
                        new LgdDetails
                        {
                            Id = detail.Id,
                            CustomerNumber = detail.CustomerNumber,
                            FacilityNumber = detail.FacilityNumber,
                            Branch = detail.Branch,
                            ProductCategory = detail.ProductCategory,
                            Segment = detail.Segment,
                            Industry = detail.Industry,
                            EarningType = detail.EarningType,
                            Nature = detail.Nature,
                            GrantDate = detail.GrantDate,
                            MaturityDate = detail.MaturityDate,
                            InterestRate = detail.InterestRate,
                            InstallmentType = detail.InstallmentType,
                            DaysPastDue = detail.DPD, // Use DPD field for VC_LGD calculations instead of DaysPastDue
                            Limit = detail.Limit,
                            TotalOS = detail.TotalOS,
                            UndisbursedAmount = detail.UndisbursedAmount,
                            InterestInSuspense = detail.InterestInSuspense,
                            CollateralType = detail.CollateralType,
                            CollateralValue = detail.CollateralValue,
                            Rescheduled = detail.Rescheduled,
                            Restructured = detail.Restructured,
                            NoOfTimesRestructured = detail.NoOfTimesRestructured,
                            UpgradedToDelinquencyBucket = detail.UpgradedToDelinquencyBucket,
                            IndividuallyImpaired = detail.IndividuallyImpaired,
                            BucketingInIndividualAssessment = detail.BucketingInIndividualAssessment,
                            Period = detail.Period,
                            FirstNplDate = detail.FirstNplDate,
                            TotalOutstandingAsAtFirstNplDate = detail.TotalOutstandingAsAtFirstNplDate,
                            ReceiptDate = detail.ReceiptDate,
                            ClosureDate = detail.ClosureDate,
                            Cashflow = detail.Cashflow,
                            Dcf = detail.Dcf,
                            DiscountedCashflows = detail.DiscountedCashflows
                        },
                        detail.VCLgdFileDetails.Year,
                        detail.VCLgdFileDetails.Period,
                        detail.VCLgdFileDetails.FileName))
                    .ToListAsync(cancellationToken);

            loadDataStopwatch.Stop();
            _logger.LogDebug("1. Load {CalculationType} Data with File Info: {ElapsedMs}ms ({ElapsedSec:F2}s) - {RecordCount} records loaded",
                calculationType, loadDataStopwatch.ElapsedMilliseconds, loadDataStopwatch.Elapsed.TotalSeconds, lgdDetailsWithFileInfo.Count);

            if (lgdDetailsWithFileInfo.Count == 0)
            {
                _logger.LogWarning("No {CalculationType} details found in database. Please run Step 1 first.", calculationType);
                return Result.Failure<HierarchicalStep2LgdCalculationResult>(Error.NotFound(
                    $"{calculationType}Calculation.Step2.NoDataFound",
                    $"No {calculationType} details found in database. Please execute Step 1 first to populate the data."));
            }

            // ==== STEP 2: CALCULATE HIERARCHICAL SUMMARIES ====
            var calculationStopwatch = Stopwatch.StartNew();
            _logger.LogDebug("2. Starting {CalculationType} hierarchical discounted cashflows summary calculation...", calculationType);

            HierarchicalStep2LgdCalculationResult hierarchicalResult =
                HierarchicalLgdCalculator.CalculateHierarchicalResult(lgdDetailsWithFileInfo, calculationType, vcPoint, vcPointsByClassification);

            calculationStopwatch.Stop();
            _logger.LogDebug("2. Calculate {CalculationType} Hierarchical Discounted Cashflows Summary: {ElapsedMs}ms ({ElapsedSec:F2}s) - {YearCount} years, {FacilityCount} unique facilities processed",
                calculationType, calculationStopwatch.ElapsedMilliseconds, calculationStopwatch.Elapsed.TotalSeconds, hierarchicalResult.YearClassifications.Count, hierarchicalResult.TotalFacilities);

            totalExecutionStopwatch.Stop();
            _logger.LogInformation("=== Step 2 {CalculationType} Hierarchical Calculation Completed in {ElapsedMs}ms ({ElapsedSec:F2} seconds) ===",
                calculationType, totalExecutionStopwatch.ElapsedMilliseconds, totalExecutionStopwatch.Elapsed.TotalSeconds);

            _logger.LogInformation("{CalculationType} Hierarchical Summary: {YearCount} years, {FacilityCount} facilities, Grand Total: {GrandTotal:C}, Portfolio Loss: {PortfolioLoss:C}, Avg LGD: {AvgLgd:P2}",
                calculationType, hierarchicalResult.YearClassifications.Count, hierarchicalResult.TotalFacilities, hierarchicalResult.GrandTotalDiscountedCashflows,
                hierarchicalResult.TotalPortfolioLoss, hierarchicalResult.AverageLgd);

            // Log year-by-year summary
            foreach (YearSummaryStatistics yearSummary in hierarchicalResult.YearSummaries.OrderBy(y => y.Year))
            {
                _logger.LogInformation("Year {Year}: {SegmentCount} segments ({Segments}), {FacilityCount} facilities, Total: {Total:C}",
                    yearSummary.Year, yearSummary.UniqueSegments, string.Join(", ", yearSummary.SegmentNames),
                    yearSummary.TotalFacilities, yearSummary.TotalDiscountedCashflows);
            }

            return Result.Success(hierarchicalResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during {CalculationType} hierarchical calculation Step 2 execution", calculationType);
            return Result.Failure<HierarchicalStep2LgdCalculationResult>(Error.Failure(
                $"{calculationType}Calculation.Step2.HierarchicalExecutionError",
                $"An error occurred during {calculationType} hierarchical calculation Step 2: {ex.Message}"));
        }
    }
}