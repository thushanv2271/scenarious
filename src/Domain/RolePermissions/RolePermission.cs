using SharedKernel;
using Domain.Roles;
using Domain.Permissions;

namespace Domain.RolePermissions;

public sealed class RolePermission : Entity
{
    public RolePermission(Guid roleId, Guid permissionId)
    {
        Id = Guid.CreateVersion7();
        RoleId = roleId;
        PermissionId = permissionId;
        AssignedAt = DateTime.UtcNow;
    }

    private RolePermission() { } // EF Core constructor

    /// <summary>
    /// Unique identifier for the role-permission assignment
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The role ID
    /// </summary>
    public Guid RoleId { get; private set; }

    /// <summary>
    /// The permission ID
    /// </summary>
    public Guid PermissionId { get; private set; }

    /// <summary>
    /// When this permission was assigned to the role
    /// </summary>
    public DateTime AssignedAt { get; private set; }

    /// <summary>
    /// Navigation property to the role
    /// </summary>
    public Role Role { get; private set; } = null!;

    /// <summary>
    /// Navigation property to the permission
    /// </summary>
    public Permission Permission { get; private set; } = null!;
}
