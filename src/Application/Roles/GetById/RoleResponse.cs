namespace Application.Roles.GetById;

public sealed record RoleResponse(
    Guid Id,
    string Name,
    string Description,
    bool IsSystemRole,
    DateTime CreatedAt,
    IReadOnlyList<string> PermissionKeys);
