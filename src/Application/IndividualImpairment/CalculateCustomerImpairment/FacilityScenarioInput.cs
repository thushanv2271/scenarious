using Application.IndividualImpairment.DTOs;

namespace Application.IndividualImpairment.CalculateCustomerImpairment;

/// <summary>
/// Represents a facility with its scenarios for customer-level calculation
/// </summary>
public sealed record FacilityScenarioInput
{
    public string FacilityNumber { get; init; } = string.Empty;
    public decimal AmortizedCost { get; init; }
    public decimal InterestRate { get; init; }
    public List<ScenarioCashFlowInput> Scenarios { get; init; } = new();
}
