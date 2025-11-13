using Application.DTOs.PD;
using Application.Models.PDSummary;

namespace Web.Api.Endpoints.PdCalculation.ExecuteStep4Extrapolation;

/// <summary>
/// Request for executing PD Calculation Step 4 - Extrapolation
/// Contains the average PD tables from Step 3 as input
/// </summary>
/// <param name="AveragePDTables">Dictionary of product categories containing segment average PD tables</param>
public sealed record Request(
    Dictionary<string, Dictionary<string, SegmentAveragePDTable>> AveragePDTables
);
