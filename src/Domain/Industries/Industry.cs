using SharedKernel;

namespace Domain.Industries;

/// <summary>
/// Represents an industry entity as a standalone reference data.
/// </summary>
public sealed class Industry : Entity
{
    /// <summary>
    /// Gets or sets the unique identifier for the industry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the industry.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the industry was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the industry was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}