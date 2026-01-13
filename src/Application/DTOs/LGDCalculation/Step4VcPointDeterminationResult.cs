namespace Application.DTOs.LGDCalculation;

/// <summary>
/// Result of Step 4 LGD calculation containing VC-Point determination results
/// </summary>
public sealed record Step4VcPointDeterminationResult
{
    /// <summary>
    /// Timestamp when the calculation was performed
    /// </summary>
    public DateTime CalculationTimestamp { get; init; }

    /// <summary>
    /// The method used to determine the VC-point
    /// </summary>
    public string DeterminationMethod { get; init; } = string.Empty;

    /// <summary>
    /// Gets the VC-point determination results per classification
    /// </summary>
    public List<ClassificationVcPointResult> ClassificationResults { get; init; } = new();
}

/// <summary>
/// VC-point determination result for a specific LGD classification
/// </summary>
public sealed record ClassificationVcPointResult
{
    /// <summary>
    /// The LGD classification / segment name (e.g., "RETAIL", "SME", "CORPORATE")
    /// </summary>
    public string Classification { get; init; } = string.Empty;

    /// <summary>
    /// The determined VC-point (years from NPL to closure date)
    /// </summary>
    public int VcPoint { get; init; }

    /// <summary>
    /// The maximum delta LGD that led to this VC-point determination
    /// </summary>
    public decimal MaxDeltaLgd { get; init; }

    /// <summary>
    /// The years from NPL to closure where the maximum delta occurred
    /// </summary>
    public int YearsAtMaxDelta { get; init; }

    /// <summary>
    /// Additional details about the calculation
    /// </summary>
    public VcPointCalculationDetails CalculationDetails { get; init; } = new();
}

/// <summary>
/// Detailed information about the VC-point calculation process
/// </summary>
public sealed record VcPointCalculationDetails
{
    /// <summary>
    /// All delta LGD values considered in the calculation
    /// </summary>
    public Dictionary<int, decimal> DeltaLgdByYears { get; init; } = new();

    /// <summary>
    /// Whether any valid data was found for this classification
    /// </summary>
    public bool HasValidData { get; init; }

    /// <summary>
    /// Notes about the calculation process
    /// </summary>
    public string Notes { get; init; } = string.Empty;
}

/// <summary>
/// Request payload for Step 4 VC-Point determination containing Step 3 yearly LGD average result
/// </summary>
public sealed record Step4VcPointDeterminationRequest
{
    /// <summary>
    /// Gets the Step 3 yearly LGD average calculation result to process
    /// </summary>
    public Step3YearlyLgdAverageResult Data { get; init; } = new();

    /// <summary>
    /// The method to use for VC-point determination
    /// </summary>
    public VcPointDeterminationMethod Method { get; init; } = VcPointDeterminationMethod.MaxDeltaLgdMinusOne;
}

/// <summary>
/// Available methods for VC-point determination
/// </summary>
public enum VcPointDeterminationMethod
{
    /// <summary>
    /// Find maximum Delta LGD, identify years, then subtract 1
    /// </summary>
    MaxDeltaLgdMinusOne = 1
}