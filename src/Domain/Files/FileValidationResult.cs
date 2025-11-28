using SharedKernel;

namespace Domain.Files;

/// <summary>
/// Represents the validation result of a uploaded file, including validation statistics and status.
/// </summary>
public sealed class FileValidationResult : Entity
{
    public int Id { get; set; }
    
    public required string Filename { get; init; }
    
    public int TotalRows { get; init; }
    
    public int TotalErrors { get; init; }
    
    public required string Status { get; init; }
    
    public DateTime CreatedOnUtc { get; init; } = DateTime.UtcNow;
    
    public DateTime? ModifiedOnUtc { get; set; }
}