using Application.DTOs.PD;
using Application.Models;

namespace Web.Api.Endpoints.PdCalculation.ExecuteStep2MatrixGeneration;

/// <summary>
/// Request for executing PD Calculation Step 2 - Matrix Generation
/// </summary>
/// <param name="TimeConfig">Time configuration containing frequency information</param>
/// <param name="DatePassedDueBuckets">List of date passed due bucket configurations</param>
/// <param name="PdConfig">PD configuration for product categories and segments</param>
public sealed record Request(
    TimeConfig TimeConfig,
    List<DatePassedDueBucket> DatePassedDueBuckets,
    PdConfigPayload PdConfig
);

/// <summary>
/// PD configuration payload
/// </summary>
/// <param name="PdConfiguration">List of PD configurations</param>
public sealed record PdConfigPayload(
  List<PDConfiguration> PdConfiguration
);
