using System;
using System.Text.Json.Nodes;

namespace Domain.PDTempData;

/// <summary>
/// Entity for temporarily storing PD setup JSON data.
/// </summary>
public sealed class PDTempData
{
    /// <summary>
    /// Gets or sets the unique identifier for the PDTempData entity.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the PD setup JSON data.
    /// </summary>
    public string PDSetupJson { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the record was created.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the identifier of the user who created the record.
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the record was last updated.
    /// </summary>
    public DateTime? UpdatedDate { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who last updated the record.
    /// </summary>
    public Guid? UpdatedBy { get; set; }
}