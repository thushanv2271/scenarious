namespace Application.Abstractions.Authentication;

public interface IPermissionCacheService
{
    void InvalidateUserPermissions(Guid userId);
    void InvalidateAllUserPermissions();
}
