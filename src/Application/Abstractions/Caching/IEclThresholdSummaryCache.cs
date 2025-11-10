using System;
using Application.EclAnalysis.CalculateThresholdSummary;

namespace Application.Abstractions.Caching;

/// <summary>
/// Cache interface for storing ECL threshold summary data
/// </summary>
public interface IEclThresholdSummaryCache
{
    /// <summary>
    /// Tries to get a cached value by key
    /// </summary>
    bool TryGetValue(string key, out EclThresholdSummaryResponse? value);

    /// <summary>
    /// Stores a value in cache with expiration time
    /// </summary>
    void Set(string key, EclThresholdSummaryResponse value, TimeSpan expiration);
}
