namespace Application.IndividualImpairment.DTOs;

public sealed record ScenarioCashFlowInput
{
    public Guid ScenarioId { get; init; }
    public string ScenarioName { get; init; } = string.Empty;
    public decimal Probability { get; init; }
    public List<CashFlowItemInput> CashFlows { get; init; } = new();
}
