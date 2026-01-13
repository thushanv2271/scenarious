using SharedKernel;

namespace Domain.Collaterals;

/// <summary>
/// Represents a collateral entity as a standalone reference data.
/// </summary>
public sealed class Collateral : Entity
{
    /// <summary>
    /// Gets or sets the unique identifier for the collateral.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the collateral.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the collateral was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the collateral was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}