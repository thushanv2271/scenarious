using Application.DTOs.PD;

namespace Web.Api.Endpoints.PdCalculation.ExecutePdCalculationPipeline;

/// <summary>
/// Response for the PD Calculation Pipeline execution
/// </summary>
/// <param name="Success">Indicates if the pipeline executed successfully</param>
/// <param name="Message">Descriptive message about the execution</param>
/// <param name="Timestamp">Timestamp when the pipeline completed</param>
/// <param name="Result">The extrapolation result containing all product categories and segments</param>
public sealed record Response(
    bool Success,
    string Message,
    DateTime Timestamp,
    PdExtrapolationResultDto Result
);
