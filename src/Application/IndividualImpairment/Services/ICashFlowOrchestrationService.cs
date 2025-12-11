using Application.IndividualImpairment.DTOs;
using SharedKernel;

namespace Application.IndividualImpairment.Services;

/// <summary>
/// Service to orchestrate cash flow generation from various sources
/// </summary>
public interface ICashFlowOrchestrationService
{
    /// <summary>
    /// Builds scenario cash flows for a facility from saved configurations
    /// </summary>
    Task<Result<List<ScenarioCashFlowInput>>> BuildScenarioCashFlowsAsync(
        string facilityNumber,
        decimal interestRate,
        CancellationToken cancellationToken = default);
}
