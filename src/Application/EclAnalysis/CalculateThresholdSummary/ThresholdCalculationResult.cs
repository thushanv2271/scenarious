// src/Application/EclAnalysis/CalculateThresholdSummary/ThresholdCalculationResult.cs
namespace Application.EclAnalysis.CalculateThresholdSummary;

/// <summary>
/// Holds the results of a threshold calculation
/// </summary>
internal sealed class ThresholdCalculationResult
{
    public string ThresholdDescription { get; init; } = string.Empty;
    public int IndividualCustomerCount { get; init; }
    public decimal IndividualCost { get; init; }
    public int CollectiveCustomerCount { get; init; }
    public decimal CollectiveCost { get; init; }
}
