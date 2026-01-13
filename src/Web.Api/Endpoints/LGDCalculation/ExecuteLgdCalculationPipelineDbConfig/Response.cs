using Application.DTOs.LGDCalculation;

namespace Web.Api.Endpoints.LgdCalculation.ExecuteLgdCalculationPipelineDbConfig;

public sealed record Response
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public required Guid SessionId { get; init; }
    public Step5FinancialYearLgdResult? Result { get; init; }
}