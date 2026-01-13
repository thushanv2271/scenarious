using SharedKernel;

namespace Domain.LGDCalculation;

/// <summary>
/// Entity for storing LGD Algorithm calculation results as JSONB
/// </summary>
public sealed class LgdAlgorithmResult : Entity
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The complete LGD algorithm result stored as JSONB for efficient querying
    /// </summary>
    public string LgdAlgorithmResultData { get; private set; } = "{}";

    public DateTime CreatedAt { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    private LgdAlgorithmResult() { } // EF Core constructor

    public static LgdAlgorithmResult Create(
        string lgdAlgorithmResultData,
        Guid createdBy)
    {
        return new LgdAlgorithmResult
        {
            Id = Guid.CreateVersion7(),
            LgdAlgorithmResultData = lgdAlgorithmResultData,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void UpdateData(string lgdAlgorithmResultData, Guid updatedBy)
    {
        LgdAlgorithmResultData = lgdAlgorithmResultData;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }
}