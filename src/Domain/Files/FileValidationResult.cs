using SharedKernel;

namespace Domain.Files;

/// <summary>
/// Represents the validation result of a uploaded file, including validation statistics and status.
/// </summary>
public sealed class FileValidationResult : Entity
{
    public int Id { get; set; }
    
    public required string Filename { get; set; }
    
    public int TotalRows { get; init; }
    
    public int TotalErrors { get; init; }
    
    public required string Status { get; init; }

    public Guid SessionId { get; init; }
    
    /// <summary>
    /// The time period this file validation belongs to (e.g., "2024-Q1", "2024-03", "2024")
    /// </summary>
    public string? TimePeriod { get; set; }
    
    /// <summary>
    /// The collective impairment type (PD or LGD)
    /// </summary>
    public string? CollectiveImpairmentType { get; set; }
    
    public DateTime CreatedOnUtc { get; init; } = DateTime.UtcNow;
    
    public DateTime? ModifiedOnUtc { get; set; }
}
