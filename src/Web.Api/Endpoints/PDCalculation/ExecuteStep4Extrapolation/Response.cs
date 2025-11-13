using Application.DTOs.PD;

namespace Web.Api.Endpoints.PdCalculation.ExecuteStep4Extrapolation;

/// <summary>
/// Response for Step 4 - PD Extrapolation
/// Contains the extrapolated PD results for all product categories and segments
/// </summary>
/// <param name="Success">Indicates if the operation was successful</param>
/// <param name="Message">Descriptive message about the operation</param>
/// <param name="Timestamp">Timestamp when the operation completed</param>
/// <param name="Result">The extrapolation result containing all product categories and their segments</param>
public sealed record Response(
    bool Success,
    string Message,
    DateTime Timestamp,
    PdExtrapolationResultDto Result
);
