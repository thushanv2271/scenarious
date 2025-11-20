namespace Application.FacilityCashFlowTypes.SaveCashFlowType;

/// <summary>
/// Represents a custom cash flow entry
/// </summary>
public sealed record CustomCashFlowDto(
    int Month,
    decimal Amount
);
