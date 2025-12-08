namespace Saral.FileProcessor.Core.Models;

public sealed record ColumnMetrics(
    string Name,
    int MissingCount,
    double MissingPercentage,
    int UniqueValues,
    string DataType
);