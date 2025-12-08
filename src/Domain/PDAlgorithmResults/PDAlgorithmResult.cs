using SharedKernel;

namespace Domain.PDAlgorithmResults;

/// <summary>
/// Entity for storing PD Algorithm calculation results as JSONB
/// </summary>
public sealed class PDAlgorithmResult : Entity
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The complete PD algorithm result stored as JSONB for efficient querying
    /// </summary>
    public string PdAlgorithmResultData { get; private set; } = "{}";

    public DateTime CreatedAt { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    private PDAlgorithmResult() { } // EF Core constructor

    public static PDAlgorithmResult Create(
        string pdAlgorithmResultData,
        Guid createdBy)
    {
        return new PDAlgorithmResult
        {
            Id = Guid.CreateVersion7(),
            PdAlgorithmResultData = pdAlgorithmResultData,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void UpdateData(string pdAlgorithmResultData, Guid updatedBy)
    {
        PdAlgorithmResultData = pdAlgorithmResultData;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }
}
