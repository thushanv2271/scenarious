using Application.Abstractions.Messaging;
using Application.IndividualImpairment.DTOs;

namespace Application.IndividualImpairment.CalculateFacilityImpairment;

public sealed record CalculateFacilityImpairmentCommand : ICommand<FacilityImpairmentResponse>
{
    public string FacilityNumber { get; init; } = string.Empty;
    public string CustomerNumber { get; init; } = string.Empty;
    public decimal AmortizedCost { get; init; }
    public decimal InterestRate { get; init; }
    public List<ScenarioCashFlowInput> Scenarios { get; init; } = new();
}
