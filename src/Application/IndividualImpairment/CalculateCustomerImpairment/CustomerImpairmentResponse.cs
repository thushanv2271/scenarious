namespace Application.IndividualImpairment.CalculateCustomerImpairment;

/// <summary>
/// Response containing customer-level impairment aggregation
/// </summary>
public sealed record CustomerImpairmentResponse
{
    public string CustomerNumber { get; init; } = string.Empty;
    public DateTime CalculationDate { get; init; }
    public int TotalFacilities { get; init; }
    public decimal CustomerAmortizedCost { get; init; }
    public decimal CustomerSumOfPV { get; init; }
    public decimal CustomerImpairmentAmount { get; init; }
    public decimal CustomerImpairmentPercentage { get; init; }
    public List<FacilityImpairmentDetail> Facilities { get; init; } = new();
}

/// <summary>
/// Facility-level impairment detail within customer aggregation
/// </summary>
public sealed record FacilityImpairmentDetail
{
    public Guid CalculationId { get; init; }
    public string FacilityNumber { get; init; } = string.Empty;
    public decimal InterestRate { get; init; }
    public decimal AmortizedCost { get; init; }
    public decimal SumOfPVOfCashFlows { get; init; }
    public decimal ImpairmentAmount { get; init; }
    public decimal ImpairmentPercentage { get; init; }
}
