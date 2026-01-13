using SharedKernel;

namespace Domain.LGDCalculation;

/// <summary>
/// Represents file details information for LGD calculation files
/// </summary>
public sealed class LgdFileDetails : Entity
{
    /// <summary>
    /// Gets the unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets the file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets the year extracted from file name
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Gets the part number of the file
    /// </summary>
    public int Part { get; set; }

    /// <summary>
    /// Gets the period extracted from the file name (e.g., "2022_01")
    /// </summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>
    /// Gets the creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets the user who created this record
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets the collection of LGD details associated with this file
    /// </summary>
    public ICollection<LgdDetails> LgdDetails { get; set; } = new List<LgdDetails>();

    /// <summary>
    /// Creates a new LgdFileDetails instance
    /// </summary>
    /// <param name="fileName">The name of the file</param>
    /// <param name="year">The year extracted from filename</param>
    /// <param name="part">The part number</param>
    /// <param name="period">The period extracted from filename</param>
    /// <param name="createdBy">The user who created this record</param>
    /// <returns>A new LgdFileDetails instance</returns>
    public static LgdFileDetails Create(
        string fileName,
        int year,
        int part,
        string period,
        string createdBy)
    {
        return new LgdFileDetails
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            Year = year,
            Part = part,
            Period = period,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }
}