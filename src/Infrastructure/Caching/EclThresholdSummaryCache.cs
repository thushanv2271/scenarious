using System;
using Application.Abstractions.Caching;
using Application.EclAnalysis.CalculateThresholdSummary;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Caching;

/// <summary>
/// In-memory cache implementation for ECL threshold summary data
/// </summary>
internal sealed class EclThresholdSummaryCache(
    IMemoryCache memoryCache,
    ILogger<EclThresholdSummaryCache> logger) : IEclThresholdSummaryCache
{
    /// <summary>
    /// Attempts to retrieve a cached value by key
    /// </summary>
    public bool TryGetValue(string key, out EclThresholdSummaryResponse? value)
    {
        bool result = memoryCache.TryGetValue(key, out EclThresholdSummaryResponse? cachedValue);
        value = cachedValue;
        return result;
    }

    /// <summary>
    /// Stores a value in cache with absolute expiration time
    /// </summary>
    public void Set(string key, EclThresholdSummaryResponse value, TimeSpan expiration)
    {
        // Configure cache entry to expire after specified duration
        MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(expiration);

        memoryCache.Set(key, value, cacheEntryOptions);

        logger.LogDebug("Cache entry set for key: {CacheKey} with expiration of {ExpirationTime}", key, expiration);
    }
}
