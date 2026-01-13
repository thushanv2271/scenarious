using System.Text.Json;

namespace Application.PDAlgorithmResults.GetPDAlgorithmResult;

/// <summary>
/// Extension methods for SegmentSummary to work with dynamic methodology data
/// </summary>
public static class SegmentSummaryExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Gets a specific methodology data by name (e.g., "method1", "method2", "method3")
    /// Returns null if the methodology doesn't exist or cannot be deserialized
    /// </summary>
    /// <param name="summary">The segment summary containing methodology data</param>
    /// <param name="methodologyName">The name of the methodology (e.g., "method1")</param>
    /// <returns>The deserialized MethodData or null if not found</returns>
    public static MethodData? GetMethodology(this SegmentSummary summary, string methodologyName)
    {
        if (!summary.Methods.TryGetValue(methodologyName, out object? methodData) || methodData == null)
        {
            return null;
        }

        try
        {
            // If it's already a JsonElement, deserialize it
            if (methodData is JsonElement jsonElement)
            {
                return JsonSerializer.Deserialize<MethodData>(jsonElement.GetRawText(), JsonOptions);
            }

            // If it's already MethodData, return it
            if (methodData is MethodData typedData)
            {
                return typedData;
            }

            // Try to serialize and deserialize to convert to MethodData
            string json = JsonSerializer.Serialize(methodData, JsonOptions);
            return JsonSerializer.Deserialize<MethodData>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets all available methodology names in the summary
    /// </summary>
    /// <param name="summary">The segment summary containing methodology data</param>
    /// <returns>List of methodology names (e.g., ["method1", "method2", "method3"])</returns>
    public static List<string> GetAvailableMethodologies(this SegmentSummary summary)
    {
        return summary.Methods.Keys.ToList();
    }

    /// <summary>
    /// Checks if a specific methodology exists in the summary
    /// </summary>
    /// <param name="summary">The segment summary containing methodology data</param>
    /// <param name="methodologyName">The name of the methodology to check</param>
    /// <returns>True if the methodology exists, false otherwise</returns>
    public static bool HasMethodology(this SegmentSummary summary, string methodologyName)
    {
        return summary.Methods.ContainsKey(methodologyName);
    }

    /// <summary>
    /// Gets all methodologies as a dictionary of name to MethodData
    /// Only includes methodologies that can be successfully deserialized
    /// </summary>
    /// <param name="summary">The segment summary containing methodology data</param>
    /// <returns>Dictionary of methodology name to MethodData</returns>
    public static Dictionary<string, MethodData> GetAllMethodologies(this SegmentSummary summary)
    {
        Dictionary<string, MethodData> result = new();

        foreach (string methodName in summary.Methods.Keys)
        {
            MethodData? deserializedData = summary.GetMethodology(methodName);
            if (deserializedData != null)
            {
                result[methodName] = deserializedData;
            }
        }

        return result;
    }
}
