namespace Application.Abstractions.Data;

/// <summary>
/// Repository for querying loan details data
/// </summary>
public interface ILoanDetailsRepository
{
    /// <summary>
    /// Gets facility details including collateral information
    /// </summary>
    Task<FacilityCollateralDetail?> GetFacilityCollateralAsync(
        string facilityNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets customer facilities with aggregated data
    /// </summary>
    Task<List<CustomerFacilityDetail>> GetCustomerFacilitiesAsync(
        string customerNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets facility basic details for validation
    /// </summary>
    Task<FacilityBasicDetail?> GetFacilityBasicDetailsAsync(
        string facilityNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets facility with complete loan details
    /// </summary>
    Task<FacilityLoanDetail?> GetFacilityLoanDetailsAsync(
        string facilityNumber,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Facility collateral information
/// </summary>
public sealed record FacilityCollateralDetail
{
    public string CustomerNumber { get; init; } = string.Empty;
    public string FacilityNumber { get; init; } = string.Empty;
    public string CollateralType { get; init; } = string.Empty;
    public decimal CollateralValue { get; init; }
}

/// <summary>
/// Customer facility aggregated details
/// </summary>
public sealed record CustomerFacilityDetail
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

/// <summary>
/// Facility basic details for validation
/// </summary>
public sealed record FacilityBasicDetail
{
    public string CustomerNumber { get; init; } = string.Empty;
    public string FacilityNumber { get; init; } = string.Empty;
    public string ProductCategory { get; init; } = string.Empty;
    public string Segment { get; init; } = string.Empty;
}

/// <summary>
/// Complete facility loan details
/// </summary>
public sealed record FacilityLoanDetail
{
    public string CustomerNumber { get; init; } = string.Empty;
    public string FacilityNumber { get; init; } = string.Empty;
    public decimal TotalOutstanding { get; init; }
    public decimal InterestRate { get; init; }
    public DateTime GrantDate { get; init; }
    public DateTime MaturityDate { get; init; }
    public string InstallmentType { get; init; } = string.Empty;
}
