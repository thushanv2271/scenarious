using Application.Abstractions.Messaging;
using Application.Users.GetUserEffectivePermissions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Infrastructure.Authorization;

internal sealed class PermissionProvider(
    IQueryHandler<GetUserEffectivePermissionsQuery, HashSet<string>> queryHandler,
    IMemoryCache memoryCache,
    ILogger<PermissionProvider> logger)
{
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(15);

    public async Task<HashSet<string>> GetForUserIdAsync(Guid userId)
    {
        string cacheKey = $"user_permissions_{userId}";

        if (memoryCache.TryGetValue(cacheKey, out object? cachedPermissions))
        {
            logger.LogDebug("Retrieved permissions from cache for user {UserId}", userId);
            return (HashSet<string>)cachedPermissions!;
        }

        GetUserEffectivePermissionsQuery query = new(userId);
        Result<HashSet<string>> result = await queryHandler.Handle(query, CancellationToken.None);

        if (result.IsFailure)
        {
            logger.LogWarning("Failed to get permissions for user {UserId}: {Error}", userId, result.Error);
            return [];
        }

        HashSet<string> permissions = result.Value;
        
        // Cache the permissions
        memoryCache.Set(cacheKey, permissions, CacheExpiration);
        
        logger.LogDebug("Retrieved and cached {PermissionCount} permissions for user {UserId}", 
            permissions.Count, userId);

        return permissions;
    }

    public void InvalidateUserPermissions(Guid userId)
    {
        string cacheKey = $"user_permissions_{userId}";
        memoryCache.Remove(cacheKey);
        logger.LogDebug("Invalidated permissions cache for user {UserId}", userId);
    }

    public void InvalidateAllUserPermissions()
    {
        // In a real implementation, you might want to track all user cache keys
        // For now, we'll rely on the 15-minute expiration
        logger.LogDebug("Cache invalidation requested for all users - relying on natural expiration");
    }
}
