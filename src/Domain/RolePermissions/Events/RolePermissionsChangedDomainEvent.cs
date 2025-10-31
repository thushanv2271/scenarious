using SharedKernel;

namespace Domain.RolePermissions.Events;

/// <summary>
/// Domain event raised when a role's permission assignments change
/// </summary>
public sealed record RolePermissionsChangedDomainEvent(
    Guid RoleId,
    IReadOnlyList<string> AddedPermissionKeys,
    IReadOnlyList<string> RemovedPermissionKeys,
    IReadOnlyList<Guid> AffectedUserIds
) : IDomainEvent;
