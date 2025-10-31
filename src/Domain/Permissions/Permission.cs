using SharedKernel;

namespace Domain.Permissions;

public sealed class Permission : Entity
{
    public Permission(Guid id, string key, string displayName, string category, string description)
    {
        Id = id;
        Key = key;
        DisplayName = displayName;
        Category = category;
        Description = description;
    }

    private Permission() { } // EF Core constructor

    /// <summary>
    /// Unique identifier for the permission
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Unique permission key (e.g., "Admin.User.Management.Create")
    /// </summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>
    /// Human-readable display name for the permission
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Category for grouping permissions (e.g., "Admin.User.Management")
    /// </summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>
    /// Description of what this permission allows
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// When this permission was created
    /// </summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public void UpdateDisplayName(string displayName)
    {
        DisplayName = displayName;
    }

    public void UpdateDescription(string description)
    {
        Description = description;
    }
}
