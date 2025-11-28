// src/Application/EclAnalysis/CalculateThresholdSummary/ThresholdCacheKeyGenerator.cs
using Domain.EclAnalysis;

namespace Application.EclAnalysis.CalculateThresholdSummary;

/// <summary>
/// Generates cache keys for threshold calculations
/// </summary>
internal static class ThresholdCacheKeyGenerator
{
    /// <summary>
    /// Generates a cache key based on threshold type and parameters
    /// </summary>
    public static string Generate(CalculateEclThresholdSummaryCommand command, string branchCode)
    {
        return command.ThresholdType switch
        {
            ThresholdType.CustomAbsolute =>
                $"ECL_Threshold_{branchCode}_CustomAbsolute_{command.IndividualSignificantThreshold}",

            ThresholdType.TopNCustomers =>
                $"ECL_Threshold_{branchCode}_TopN_{command.TopNCount}",

            ThresholdType.CumulativePercentage =>
                $"ECL_Threshold_{branchCode}_Cumulative_{command.CumulativePercentageThreshold}",

            _ => $"ECL_Threshold_{branchCode}_Unknown"
        };
    }
}
