namespace Web.Api.Endpoints.PdCalculation.ExecutePdCalculationPipelineDbConfig;

public sealed record Request
{
    public required Guid SessionId { get; init; }
}
