using Application.Models;

namespace Web.Api.Endpoints.PdCalculation.ExecuteStep1DataPreparation;

/// <summary>
/// Request for executing PD Calculation Step 1 - Data Preparation
/// </summary>
/// <param name="QuarterEndedDates">Dictionary mapping file identifiers to quarter ended dates</param>
/// <param name="DatePassedDueBuckets">List of date passed due bucket configurations</param>
/// <param name="FinalBucketPayload">Final bucket calculation configuration</param>
/// <param name="TimeConfig">Time configuration containing frequency information</param>
public sealed record Request(
    Dictionary<string, DateTime> QuarterEndedDates,
    List<DatePassedDueBucket> DatePassedDueBuckets,
    FinalBucketPayload FinalBucketPayload,
    TimeConfig TimeConfig
);
