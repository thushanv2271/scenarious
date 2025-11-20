namespace Application.CashFlowProjections.GetCustomerFacilities;

/// <summary>
/// Response containing facility information for a customer
/// </summary>
public sealed record CustomerFacilityResponse
{
    public string CustomerNumber { get; init; } = string.Empty;
    public string FacilityNumber { get; init; } = string.Empty;
    public string ProductCategory { get; init; } = string.Empty;
    public string Segment { get; init; } = string.Empty;
    public string Branch { get; init; } = string.Empty;
    public decimal TotalOutstanding { get; init; }
    public decimal InterestRate { get; init; }
    public DateTime GrantDate { get; init; }
    public DateTime MaturityDate { get; init; }
    public int DaysPastDue { get; init; }
    public string BucketLabel { get; init; } = string.Empty;
}
