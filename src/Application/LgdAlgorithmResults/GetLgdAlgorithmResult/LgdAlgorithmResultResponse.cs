using System.Text.Json.Serialization;

namespace Application.LgdAlgorithmResults.GetLgdAlgorithmResult;

/// <summary>
/// Response containing the LGD Algorithm Result data
/// </summary>
public sealed record LgdAlgorithmResultResponse
{
    public Guid Id { get; init; }
    public LgdAlgorithmData Data { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public Guid CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public Guid? UpdatedBy { get; init; }
}

/// <summary>
/// Root structure for LGD Algorithm data
/// This structure will depend on the actual LGD algorithm result format
/// For now, using a generic approach similar to PD Algorithm Results
/// </summary>
public sealed record LgdAlgorithmData
{
    [JsonPropertyName("results")]
    public Dictionary<string, object> Results { get; init; } = new();

    /// <summary>
    /// Uses JsonExtensionData to capture the entire LGD algorithm result structure
    /// This allows flexibility for different LGD result formats
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object> AdditionalData { get; init; } = new();
}