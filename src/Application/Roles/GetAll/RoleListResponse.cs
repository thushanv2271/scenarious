using Domain.Permissions;

namespace Application.Roles.GetAll;

public sealed record RoleListResponse(
    Guid Id,
    string Name,
    string Description,
    bool IsSystemRole,
    DateTime CreatedAt,
    int PermissionCount,
    List<Permission> Permissions
    );
