using SharedKernel;

namespace Domain.Roles;

public sealed class Role : Entity
{
    public Role(Guid id, string name, string description, bool isSystemRole = false)
    {
        Id = id;
        Name = name;
        Description = description;
        IsSystemRole = isSystemRole;
        CreatedAt = DateTime.UtcNow;
    }

    private Role() { } // EF Core constructor

    /// <summary>
    /// Unique identifier for the role
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Role name (e.g., "Administrator", "Finance Manager")
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Description of the role's purpose
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Whether this is a system role that cannot be deleted
    /// </summary>
    public bool IsSystemRole { get; private set; }

    /// <summary>
    /// Whether this role is currently active
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// When this role was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When this role was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    public void UpdateName(string name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDescription(string description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string description)
    {
        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
}
