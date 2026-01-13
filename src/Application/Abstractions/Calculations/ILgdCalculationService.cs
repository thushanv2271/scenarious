using Application.DTOs.LGDCalculation;
using Application.Models;
using SharedKernel;

namespace Application.Abstractions.Calculations;

/// <summary>
/// Interface for LGD Calculation service
/// </summary>
public interface ILgdCalculationService
{
    /// <summary>
    /// Executes step 1 of LGD calculation
    /// </summary>
    /// <param name="createdBy">User who initiated the calculation</param>
    /// <param name="calculationType">Type of calculation to perform (LGD or VC_LGD)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    Task<Result> ExecuteStep1Async(string createdBy, LgdCalculationType calculationType = LgdCalculationType.LGD, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes step 2 of LGD calculation with hierarchical structure: Year > LGD Classification (Segment) > Facility
    /// </summary>
    /// <param name="calculationType">Type of calculation to perform (LGD or VC_LGD)</param>
    /// <param name="vcPoint">Legacy VC Point threshold value in years. Only used for VC_LGD calculations for backward compatibility.</param>
    /// <param name="vcPointsByClassification">VC Point threshold values by classification. Only used for VC_LGD calculations. Takes precedence over vcPoint parameter.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing hierarchical facility summaries organized by year, segment, and facility</returns>
    Task<Result<HierarchicalStep2LgdCalculationResult>> ExecuteStep2Async(LgdCalculationType calculationType = LgdCalculationType.LGD, decimal? vcPoint = null, Dictionary<string, decimal>? vcPointsByClassification = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes step 3 of LGD calculation: Yearly LGD average calculation that computes LGD averages and changes by financial year
    /// </summary>
    /// <param name="step2Result">Step 2 hierarchical calculation result containing the source data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing yearly LGD average calculation result</returns>
    Task<Result<Step3YearlyLgdAverageResult>> ExecuteStep3Async(HierarchicalStep2LgdCalculationResult step2Result, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes step 4 of LGD calculation: VC-point determination that identifies optimal conversion point
    /// </summary>
    /// <param name="step3Result">Step 3 yearly LGD average calculation result containing the source data</param>
    /// <param name="method">The method to use for VC-point determination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing VC-point determination result</returns>
    Task<Result<Step4VcPointDeterminationResult>> ExecuteStep4Async(Step3YearlyLgdAverageResult step3Result, VcPointDeterminationMethod method = VcPointDeterminationMethod.MaxDeltaLgdMinusOne, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes step 5 of LGD calculation: Financial year-based LGD analysis using Step 2 hierarchical data
    /// </summary>
    /// <param name="step2Result">Step 2 hierarchical calculation result containing the source data</param>
    /// <param name="financialYearEnds">List of financial year end dates for analysis</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing financial year-based LGD analysis</returns>
    Task<Result<Step5FinancialYearLgdResult>> ExecuteStep5Async(HierarchicalStep2LgdCalculationResult step2Result, List<DateTime> financialYearEnds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes step 6 of LGD calculation: Sum of two Step 5 financial year LGD analysis results
    /// </summary>
    /// <param name="payload1">First Step 5 financial year LGD result</param>
    /// <param name="payload2">Second Step 5 financial year LGD result</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the summed Step 5 financial year LGD result</returns>
    Task<Result<Step5FinancialYearLgdResult>> ExecuteStep6Async(Step5FinancialYearLgdResult payload1, Step5FinancialYearLgdResult payload2, CancellationToken cancellationToken = default);
}