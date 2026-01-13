using Application.DTOs.LGDCalculation;
using SharedKernel;

namespace Application.Abstractions.Pipeline;

/// <summary>
/// Orchestration-level service for executing the complete LGD calculation pipeline
/// </summary>
public interface ILgdPipelineService
{
    /// <summary>
    /// Executes the complete LGD calculation pipeline from end-to-end by:
    /// 1. Fetching all required LGD configuration from the database
    /// 2. Executing Step 1: LGD and VC_LGD data preparation
    /// 3. Executing Step 2: Discounted cashflow summary generation
    /// 4. Executing Step 3: Yearly LGD average calculation (if VC points not provided)
    /// 5. Executing Step 4: VC-point determination (if VC points not provided)
    /// 6. Executing Step 2 VC_LGD: Discounted cashflow summary with VC points
    /// 7. Executing Step 5: Financial year analysis for both LGD and VC_LGD results
    /// 8. Executing Step 6: Final result combination
    /// </summary>
    /// <param name="createdBy">User who initiated the pipeline execution</param>
    /// <param name="sessionId">Session ID for tracking progress</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the final LGD calculation results</returns>
    Task<Result<Step5FinancialYearLgdResult>> RunPipelineFromDbAsync(
        string createdBy,
        Guid sessionId,
        CancellationToken cancellationToken = default);
}