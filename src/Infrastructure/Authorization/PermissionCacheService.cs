using Application.Abstractions.Authentication;

namespace Infrastructure.Authorization;

internal sealed class PermissionCacheService(PermissionProvider permissionProvider) 
    : IPermissionCacheService
{
    public void InvalidateUserPermissions(Guid userId)
    {
        permissionProvider.InvalidateUserPermissions(userId);
    }

    public void InvalidateAllUserPermissions()
    {
        permissionProvider.InvalidateAllUserPermissions();
    }
}
