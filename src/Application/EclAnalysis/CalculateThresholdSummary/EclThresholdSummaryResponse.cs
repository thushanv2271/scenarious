namespace Application.EclAnalysis.CalculateThresholdSummary;

/// <summary>
/// Response containing customer count and total cost for an impairment category
/// </summary>
public sealed class ImpairmentCategoryResponse
{
    // Number of customers in this category
    public int CustomerCount { get; init; }

    // Total outstanding balance for this category
    public decimal AmortizedCost { get; init; }
}
