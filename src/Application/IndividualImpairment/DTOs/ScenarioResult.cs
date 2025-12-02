namespace Application.IndividualImpairment.DTOs;

public sealed record ScenarioResult
{
    public Guid ScenarioId { get; init; }
    public string ScenarioName { get; init; } = string.Empty;
    public decimal Probability { get; init; }
    public List<DiscountedCashFlowItem> DiscountedCashFlows { get; init; } = new();
    public decimal ScenarioSumOfPV { get; init; }
    public decimal WeightedPV { get; init; }
}
