namespace Saral.FileProcessor.Core.Models;

public sealed record AnalysisResult(
    FileLoadContext FileContext,
    ImmutableArray<ColumnMetrics> Columns,
    ImmutableArray<RowMetrics> RowsWithMissing,
    ImmutableArray<MissingDistribution> MissingDistribution,
    SummaryStatistics Summary,
    AnalysisOptions Options,
    ValidationSummary? ValidationSummary = null
)
{
    public int TotalRows => FileContext.Data.RowCount;
    public int TotalColumns => FileContext.Data.ColumnCount;
}