namespace Application.IndividualImpairment.DTOs;

public sealed record CashFlowItemInput
{
    public int Month { get; init; }
    public decimal CashFlowAmount { get; init; }
}
