namespace Application.LgdAlgorithmResults.UpdateLgdAlgorithmResult;

/// <summary>
/// Request DTO for updating LGD Algorithm Result JSON data
/// </summary>
public sealed record UpdateLgdAlgorithmResultRequest
{
    /// <summary>
    /// The complete LGD algorithm result data as JSON string
    /// </summary>
    public string LgdAlgorithmResultData { get; init; } = string.Empty;
}