using Application.IndividualImpairment.DTOs;
using SharedKernel;

namespace Application.Abstractions.Services;

/// <summary>
/// Service for generating cash flows for impairment calculations
/// Implements Single Responsibility Principle
/// </summary>
public interface ICashFlowGeneratorService
{
    /// <summary>
    /// Generates scenarios with cash flows for a facility
    /// </summary>
    /// <param name="facilityNumber">Facility identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of scenarios with generated cash flows</returns>
    Task<Result<List<ScenarioCashFlowInput>>> GenerateScenariosForFacilityAsync(
        string facilityNumber,
        CancellationToken cancellationToken = default);
}
