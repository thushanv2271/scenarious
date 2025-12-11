using Application.DTOs.PD;
using SharedKernel;

namespace Application.Abstractions.Pipeline;

/// <summary>
/// Orchestration-level service for executing the complete PD calculation pipeline
/// </summary>
public interface IPDPipelineService
{
    /// <summary>
    /// Executes the complete PD calculation pipeline from end-to-end by:
    /// 1. Fetching all required PD configuration from the database
    /// 2. Executing Step 1: File extraction, calculation, and DB insertion
/// 3. Executing Step 2: Migration matrix generation with counts and percentages
    /// 4. Executing Step 3: PD Summary table generation from migration matrices
    /// 5. Executing Step 4: PD extrapolation tables for all three methods
    /// </summary>
  /// <param name="createdBy">User who initiated the pipeline execution</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the final aggregated PD calculation results</returns>
    Task<Result<PdExtrapolationResultDto>> RunPipelineFromDbAsync(
        string createdBy,
        Guid SessionId,
  CancellationToken cancellationToken = default);
}
