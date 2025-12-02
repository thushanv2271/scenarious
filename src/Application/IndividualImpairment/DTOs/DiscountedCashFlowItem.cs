namespace Application.IndividualImpairment.DTOs;

public sealed record DiscountedCashFlowItem
{
    public int Month { get; init; }
    public decimal CashFlowAmount { get; init; }
    public decimal DiscountFactor { get; init; }
    public decimal PresentValue { get; init; }
}
