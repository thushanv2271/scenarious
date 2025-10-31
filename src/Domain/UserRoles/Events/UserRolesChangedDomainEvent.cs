using SharedKernel;

namespace Domain.UserRoles.Events;

/// <summary>
/// Domain event raised when a user's role assignments change
/// </summary>
public sealed record UserRolesChangedDomainEvent(
    Guid UserId,
    IReadOnlyList<Guid> AddedRoleIds,
    IReadOnlyList<Guid> RemovedRoleIds
) : IDomainEvent;
