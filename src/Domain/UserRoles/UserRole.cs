using SharedKernel;
using Domain.Users;
using Domain.Roles;

namespace Domain.UserRoles;

public sealed class UserRole : Entity
{
    public UserRole(Guid userId, Guid roleId)
    {
        Id = Guid.CreateVersion7();
        UserId = userId;
        RoleId = roleId;
        AssignedAt = DateTime.UtcNow;
    }

    private UserRole() { } // EF Core constructor

    /// <summary>
    /// Unique identifier for the user-role assignment
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The user ID
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The role ID
    /// </summary>
    public Guid RoleId { get; private set; }

    /// <summary>
    /// When this role was assigned to the user
    /// </summary>
    public DateTime AssignedAt { get; private set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    public User User { get; private set; } = null!;

    /// <summary>
    /// Navigation property to the role
    /// </summary>
    public Role Role { get; private set; } = null!;
}
