namespace Web.Api.Endpoints.LgdCalculation.ExecuteLgdCalculationPipelineDbConfig;

public sealed record Request
{
    public required Guid SessionId { get; init; }
}