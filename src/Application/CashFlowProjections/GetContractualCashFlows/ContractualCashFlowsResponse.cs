namespace Application.CashFlowProjections.GetContractualCashFlows;

/// <summary>
/// Response containing contractual cash flow data from portfolio snapshot
/// </summary>
public sealed record ContractualCashFlowsResponse
{
    public string FacilityNumber { get; init; } = string.Empty;
    public string CustomerNumber { get; init; } = string.Empty;
    public decimal AmortisedCost { get; init; }
    public decimal InterestRate { get; init; }
    public DateTime GrantDate { get; init; }
    public DateTime MaturityDate { get; init; }
    public int TenureMonths { get; init; }
    public string InstallmentType { get; init; } = string.Empty;
    public List<MonthlyCashFlow> ProjectedCashFlows { get; init; } = new();
}

/// <summary>
/// Monthly cash flow projection
/// </summary>
public sealed record MonthlyCashFlow
{
    public int Month { get; init; }
    public decimal PrincipalAmount { get; init; }
    public decimal InterestAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime PaymentDate { get; init; }
}
