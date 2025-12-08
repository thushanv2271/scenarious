namespace Saral.FileProcessor.Core.Models;

public sealed record RowMetrics(
    int Index,
    int MissingCount,
    double MissingPercentage,
    IReadOnlyDictionary<string, object?> Values
);