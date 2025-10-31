using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Domain.MasterData;

/// <summary>
/// Entity representing Segment Master data.
/// </summary>
public sealed class SegmentMaster
{
    /// <summary>
    /// Gets or sets the unique identifier for the segment.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the segment name.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Segment { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of subsegments.
    /// </summary>
    [Required]
    public List<string> SubSegments { get; set; } = new();

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last modification timestamp.
    /// </summary>
    public DateTime? ModifiedAt { get; set; }
}