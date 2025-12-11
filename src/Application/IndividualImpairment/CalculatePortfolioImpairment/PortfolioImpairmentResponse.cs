namespace Application.IndividualImpairment.CalculatePortfolioImpairment;

/// <summary>
/// Response containing portfolio-level impairment aggregation
/// </summary>
public sealed record PortfolioImpairmentResponse
{
    public DateTime CalculationDate { get; init; }
    public string? BranchCode { get; init; }
    public int TotalCustomers { get; init; }
    public int TotalFacilities { get; init; }
    public decimal PortfolioAmortizedCost { get; init; }
    public decimal PortfolioSumOfPV { get; init; }
    public decimal PortfolioImpairmentAmount { get; init; }
    public decimal PortfolioImpairmentPercentage { get; init; }
    public List<CustomerImpairmentSummary> Customers { get; init; } = new();
    public List<string> FailedCustomers { get; init; } = new();
}

/// <summary>
/// Customer-level summary within portfolio aggregation
/// </summary>
public sealed record CustomerImpairmentSummary
{
    public string CustomerNumber { get; init; } = string.Empty;
    public int FacilityCount { get; init; }
    public decimal CustomerAmortizedCost { get; init; }
    public decimal CustomerSumOfPV { get; init; }
    public decimal CustomerImpairmentAmount { get; init; }
    public decimal CustomerImpairmentPercentage { get; init; }
}
