using SharedKernel;

namespace Domain.PDCalculation;

/// <summary>
/// Represents loan details extracted from PD calculation files
/// </summary>
public sealed class LoanDetails : Entity
{
    /// <summary>
    /// Gets the unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets the file details ID this loan belongs to
    /// </summary>
    public Guid FileDetailsId { get; set; }

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
    /// Gets the maturity date
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
    /// Gets the calculated remaining maturity in appropriate units based on frequency
    /// </summary>
    public int RemainingMaturityYears { get; set; }

    /// <summary>
    /// Gets the calculated bucket label based on days past due
    /// </summary>
    public string BucketLabel { get; set; } = string.Empty;

    /// <summary>
    /// Gets the final bucket determined by customer-wise grouping and sorting logic
    /// </summary>
    public string FinalBucket { get; set; } = string.Empty;

    /// <summary>
    /// Gets the file details this loan belongs to
    /// </summary>
    public FileDetails FileDetails { get; set; } = null!;

    /// <summary>
    /// Creates a new LoanDetails instance from a creation request
    /// </summary>
    /// <param name="request">The loan details creation request</param>
    /// <returns>A new LoanDetails instance</returns>
    public static LoanDetails Create(LoanDetailsCreationRequest request)
    {
        return new LoanDetails
        {
            Id = Guid.NewGuid(),
            FileDetailsId = request.FileDetailsId,
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
            RemainingMaturityYears = request.RemainingMaturityYears,
            BucketLabel = request.BucketLabel,
            FinalBucket = request.FinalBucket
        };
    }
}

/// <summary>
/// Request object for creating loan details
/// </summary>
public sealed record LoanDetailsCreationRequest(
    Guid FileDetailsId,
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
    int RemainingMaturityYears,
    string BucketLabel,
    string FinalBucket);
