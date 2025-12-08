namespace Saral.FileProcessor.Core.Models;

public sealed record SummaryStatistics(
    long TotalCells,
    long TotalMissing,
    double MissingPercentage,
    double DataQualityScore,
    int ColumnsWithMissing,
    int RowsWithMissing
);