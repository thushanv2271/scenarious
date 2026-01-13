using Application.DTOs.LGDCalculation;

namespace Web.Api.Endpoints.LgdCalculation.ExecuteStep6TwoPayloadSum;

/// <summary>
/// Request model for Step 6 two payload sum analysis
/// </summary>
public sealed record ExecuteStep6TwoPayloadSumRequest
{
    /// <summary>
    /// First Step 5 financial year LGD result
    /// </summary>
    public required Step5FinancialYearLgdResult Payload1 { get; init; }

    /// <summary>
    /// Second Step 5 financial year LGD result
    /// </summary>
    public required Step5FinancialYearLgdResult Payload2 { get; init; }
}