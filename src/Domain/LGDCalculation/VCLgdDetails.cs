using SharedKernel;

namespace Domain.LGDCalculation;

/// <summary>
/// Represents VC LGD details extracted from VC LGD calculation files
/// </summary>
public sealed class VCLgdDetails : Entity
{
    /// <summary>
    /// Gets the unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets the file details ID this VC LGD record belongs to
    /// </summary>
    public Guid VCLgdFileDetailsId { get; set; }

    /// <summary>
    /// Gets the customer number
    /// </summary>
    public string CustomerNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets the facility number
    /// </summary>
    public string FacilityNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets the branch
    /// </summary>
    public string Branch { get; set; } = string.Empty;

    /// <summary>
    /// Gets the product category
    /// </summary>
    public string ProductCategory { get; set; } = string.Empty;

    /// <summary>
    /// Gets the segment
    /// </summary>
    public string Segment { get; set; } = string.Empty;

    /// <summary>
    /// Gets the industry
    /// </summary>
    public string Industry { get; set; } = string.Empty;

    /// <summary>
    /// Gets the earning type
    /// </summary>
    public string EarningType { get; set; } = string.Empty;

    /// <summary>
    /// Gets the nature
    /// </summary>
    public string Nature { get; set; } = string.Empty;

    /// <summary>
    /// Gets the grant date
    /// </summary>
    public DateTime GrantDate { get; set; }

    /// <summary>
    /// Gets the maturity date / expiry date
    /// </summary>
    public DateTime MaturityDate { get; set; }

    /// <summary>
    /// Gets the interest rate
    /// </summary>
    public decimal InterestRate { get; set; }

    /// <summary>
    /// Gets the installment type
    /// </summary>
    public string InstallmentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets the days past due
    /// </summary>
    public int DaysPastDue { get; set; }

    /// <summary>
    /// Gets the DPD (Days Past Due) for VC LGD calculations
    /// </summary>
    public int DPD { get; set; }

    /// <summary>
    /// Gets the limit
    /// </summary>
    public decimal Limit { get; set; }

    /// <summary>
    /// Gets the total outstanding
    /// </summary>
    public decimal TotalOS { get; set; }

    /// <summary>
    /// Gets the undisbursed amount
    /// </summary>
    public decimal UndisbursedAmount { get; set; }

    /// <summary>
    /// Gets the interest in suspense
    /// </summary>
    public decimal InterestInSuspense { get; set; }

    /// <summary>
    /// Gets the collateral type
    /// </summary>
    public string CollateralType { get; set; } = string.Empty;

    /// <summary>
    /// Gets the collateral value
    /// </summary>
    public decimal CollateralValue { get; set; }

    /// <summary>
    /// Gets whether the loan is rescheduled
    /// </summary>
    public bool Rescheduled { get; set; }

    /// <summary>
    /// Gets whether the loan is restructured
    /// </summary>
    public bool Restructured { get; set; }

    /// <summary>
    /// Gets the number of times restructured
    /// </summary>
    public int NoOfTimesRestructured { get; set; }

    /// <summary>
    /// Gets whether upgraded to delinquency bucket
    /// </summary>
    public bool UpgradedToDelinquencyBucket { get; set; }

    /// <summary>
    /// Gets whether individually impaired
    /// </summary>
    public bool IndividuallyImpaired { get; set; }

    /// <summary>
    /// Gets the bucketing in individual assessment
    /// </summary>
    public string BucketingInIndividualAssessment { get; set; } = string.Empty;

    /// <summary>
    /// Gets the period
    /// </summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>
    /// Gets the first NPL date
    /// </summary>
    public DateTime? FirstNplDate { get; set; }

    /// <summary>
    /// Gets the total outstanding as at first NPL date
    /// </summary>
    public decimal TotalOutstandingAsAtFirstNplDate { get; set; }

    /// <summary>
    /// Gets the receipt date
    /// </summary>
    public DateTime ReceiptDate { get; set; }

    /// <summary>
    /// Gets the closure date
    /// </summary>
    public DateTime ClosureDate { get; set; }

    /// <summary>
    /// Gets the cashflow amount
    /// </summary>
    public decimal Cashflow { get; set; }

    /// <summary>
    /// Gets the calculated discount factor (DCF) based on receipt date, first NPL date, and interest rate
    /// </summary>
    public decimal Dcf { get; set; }

    /// <summary>
    /// Gets the calculated discounted cashflows (Cashflows * DCF)
    /// </summary>
    public decimal DiscountedCashflows { get; set; }

    /// <summary>
    /// Gets the file details this VC LGD record belongs to
    /// </summary>
    public VCLgdFileDetails VCLgdFileDetails { get; set; } = null!;

    /// <summary>
    /// Creates a new VCLgdDetails instance from a creation request
    /// </summary>
    /// <param name="request">The VC LGD details creation request</param>
    /// <returns>A new VCLgdDetails instance</returns>
    public static VCLgdDetails Create(VCLgdDetailsCreationRequest request)
    {
        return new VCLgdDetails
        {
            Id = Guid.NewGuid(),
            VCLgdFileDetailsId = request.VCLgdFileDetailsId,
            CustomerNumber = request.CustomerNumber,
            FacilityNumber = request.FacilityNumber,
            Branch = request.Branch,
            ProductCategory = request.ProductCategory,
            Segment = request.Segment,
            Industry = request.Industry,
            EarningType = request.EarningType,
            Nature = request.Nature,
            GrantDate = request.GrantDate,
            MaturityDate = request.MaturityDate,
            InterestRate = request.InterestRate,
            InstallmentType = request.InstallmentType,
            DaysPastDue = request.DaysPastDue,
            DPD = request.DPD,
            Limit = request.Limit,
            TotalOS = request.TotalOS,
            UndisbursedAmount = request.UndisbursedAmount,
            InterestInSuspense = request.InterestInSuspense,
            CollateralType = request.CollateralType,
            CollateralValue = request.CollateralValue,
            Rescheduled = request.Rescheduled,
            Restructured = request.Restructured,
            NoOfTimesRestructured = request.NoOfTimesRestructured,
            UpgradedToDelinquencyBucket = request.UpgradedToDelinquencyBucket,
            IndividuallyImpaired = request.IndividuallyImpaired,
            BucketingInIndividualAssessment = request.BucketingInIndividualAssessment,
            Period = request.Period,
            FirstNplDate = request.FirstNplDate,
            TotalOutstandingAsAtFirstNplDate = request.TotalOutstandingAsAtFirstNplDate,
            ReceiptDate = request.ReceiptDate,
            ClosureDate = request.ClosureDate,
            Cashflow = request.Cashflow,
            Dcf = request.Dcf,
            DiscountedCashflows = request.DiscountedCashflows
        };
    }
}

/// <summary>
/// Request object for creating VC LGD details
/// </summary>
public sealed record VCLgdDetailsCreationRequest(
    Guid VCLgdFileDetailsId,
    string CustomerNumber,
    string FacilityNumber,
    string Branch,
    string ProductCategory,
    string Segment,
    string Industry,
    string EarningType,
    string Nature,
    DateTime GrantDate,
    DateTime MaturityDate,
    decimal InterestRate,
    string InstallmentType,
    int DaysPastDue,
    int DPD,
    decimal Limit,
    decimal TotalOS,
    decimal UndisbursedAmount,
    decimal InterestInSuspense,
    string CollateralType,
    decimal CollateralValue,
    bool Rescheduled,
    bool Restructured,
    int NoOfTimesRestructured,
    bool UpgradedToDelinquencyBucket,
    bool IndividuallyImpaired,
    string BucketingInIndividualAssessment,
    string Period,
    DateTime? FirstNplDate,
    decimal TotalOutstandingAsAtFirstNplDate,
    DateTime ReceiptDate,
    DateTime ClosureDate,
    decimal Cashflow,
    decimal Dcf,
    decimal DiscountedCashflows);