namespace Web.Api.Endpoints.PdCalculation.ExecutePdCalculationPipelineDbConfig;

public sealed record Response
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
}
