using Application.DTOs.PD;
using Application.Models;

namespace Web.Api.Endpoints.PdCalculation.ExecutePdCalculationPipeline;

/// <summary>
/// Request for executing the full PD Calculation Pipeline
/// </summary>
/// <param name="QuarterEndedDates">Dictionary mapping file identifiers to quarter ended dates</param>
/// <param name="DatePassedDueBuckets">List of date passed due bucket configurations</param>
/// <param name="FinalBucketPayload">Final bucket calculation configuration</param>
/// <param name="TimeConfig">Time configuration containing frequency information</param>
/// <param name="PdConfig">PD configuration for product categories and segments</param>
public sealed record Request(
    Dictionary<string, DateTime> QuarterEndedDates,
    List<DatePassedDueBucket> DatePassedDueBuckets,
    FinalBucketPayload FinalBucketPayload,
    TimeConfig TimeConfig,
    PdConfigPayload PdConfig
);

/// <summary>
/// PD configuration payload
/// </summary>
/// <param name="PdConfiguration">List of PD configurations</param>
public sealed record PdConfigPayload(
    List<PDConfiguration> PdConfiguration
);
