namespace Application.CashFlowProjections.GetCollateralCashFlowData;

/// <summary>
/// Response containing collateral and historical cash flow data
/// </summary>
public sealed record CollateralCashFlowDataResponse
{
    public string FacilityNumber { get; init; } = string.Empty;
    public string CustomerNumber { get; init; } = string.Empty;
    public CollateralData Collateral { get; init; } = new();
    public LastQuarterCashFlowData? LastQuarterCashFlows { get; init; }
}

/// <summary>
/// Collateral information
/// </summary>
public sealed record CollateralData
{
    public string CollateralType { get; init; } = string.Empty;
    public decimal CollateralValue { get; init; }
    public decimal HaircutPercentage { get; init; } = 0.40m; // Default 40%
    public decimal NetRealizableValue { get; init; }
}

/// <summary>
/// Last quarter cash flow data from uploaded files
/// </summary>
public sealed record LastQuarterCashFlowData
{
    public Guid UploadedFileId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public DateTime UploadedAt { get; init; }
    public List<HistoricalCashFlow> CashFlows { get; init; } = new();
}

/// <summary>
/// Historical cash flow entry
/// </summary>
public sealed record HistoricalCashFlow
{
    public DateTime Date { get; init; }
    public decimal Amount { get; init; }
    public string Description { get; init; } = string.Empty;
}
