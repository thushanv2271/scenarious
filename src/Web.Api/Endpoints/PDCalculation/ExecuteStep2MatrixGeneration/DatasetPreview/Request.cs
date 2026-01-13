using Application.DTOs.PD;
using Application.Models;

namespace Web.Api.Endpoints.PdCalculation.ExecuteStep2MatrixGeneration.DatasetPreview;

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
