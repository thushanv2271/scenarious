using SharedKernel;

namespace Domain.PDCalculation;

/// <summary>
/// Represents file details information for PD calculation files
/// </summary>
public sealed class FileDetails : Entity
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
    /// Gets the frequency type of the file
    /// </summary>
    public FrequencyType Frequency { get; set; }

    /// <summary>
    /// Gets the part number of the file
    /// </summary>
    public int Part { get; set; }

    /// <summary>
    /// Gets the quarter ended date
    /// </summary>
    public DateTime QuarterEndedDate { get; set; }

    /// <summary>
    /// Gets the period extracted from the file name (e.g., "2024-01", "2024Q1", "2022")
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
    /// Gets the collection of loan details associated with this file
    /// </summary>
    public ICollection<LoanDetails> LoanDetails { get; set; } = new List<LoanDetails>();

    /// <summary>
    /// Creates a new FileDetails instance
    /// </summary>
    /// <param name="fileName">The name of the file</param>
    /// <param name="frequency">The frequency type</param>
    /// <param name="part">The part number</param>
    /// <param name="quarterEndedDate">The quarter ended date</param>
    /// <param name="period">The period extracted from filename</param>
    /// <param name="createdBy">The user who created this record</param>
    /// <returns>A new FileDetails instance</returns>
    public static FileDetails Create(
        string fileName,
        FrequencyType frequency,
        int part,
        DateTime quarterEndedDate,
        string period,
        string createdBy)
    {
        return new FileDetails
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            Frequency = frequency,
            Part = part,
            QuarterEndedDate = quarterEndedDate,
            Period = period,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }
}
