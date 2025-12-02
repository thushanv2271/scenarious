using Application.IndividualImpairment.DTOs;

namespace Application.IndividualImpairment.CalculateFacilityImpairment;

public sealed record FacilityImpairmentResponse
{
    public string FacilityNumber { get; init; } = string.Empty;
    public string CustomerNumber { get; init; } = string.Empty;
    public DateTime CalculationDate { get; init; }
    public decimal InterestRate { get; init; }
    public decimal AmortizedCost { get; init; }
    public decimal SumOfPVOfCashFlows { get; init; }
    public decimal ImpairmentAmount { get; init; }
    public decimal ImpairmentPercentage { get; init; }
    public List<ScenarioResult> Scenarios { get; init; } = new();
    public CalculationSummary CalculationSummary { get; init; } = new();
}

public sealed record CalculationSummary
{
    public decimal TotalWeightedPV { get; init; }
    public string Formula { get; init; } = string.Empty;
    public string Calculation { get; init; } = string.Empty;
}
