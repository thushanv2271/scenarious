namespace Application.EclAnalysis.CalculateThresholdSummary;

/// <summary>
/// Response containing ECL threshold summary categorized by individual and collective impairment
/// </summary>
public sealed class EclThresholdSummaryResponse
{
    // Branch identifier code
    public string BranchCode { get; init; } = string.Empty;

    // Branch display name
    public string BranchName { get; init; } = string.Empty;

    // Customers with exposure above threshold (individually significant)
    public ImpairmentCategoryResponse Individual { get; init; } = new();

    // Customers with exposure below threshold (collectively assessed)
    public ImpairmentCategoryResponse Collective { get; init; } = new();

    // Combined totals for all customers
    public ImpairmentCategoryResponse GrandTotal { get; init; } = new();
}
