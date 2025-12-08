namespace Saral.FileProcessor.Core.Analysis;

public sealed class DataQualityAnalyzer : IDataQualityAnalyzer
{
    private readonly IDataValidator? _dataValidator;

    public DataQualityAnalyzer(IDataValidator? dataValidator = null)
    {
        _dataValidator = dataValidator;
    }

    public AnalysisResult Analyze(FileLoadContext loadContext, AnalysisOptions options)
    {
        ArgumentNullException.ThrowIfNull(loadContext);
        ArgumentNullException.ThrowIfNull(options);

        Frame<int, string> frame = loadContext.Data;
        var columnMetrics = CreateColumnMetrics(frame).ToImmutableArray();
        var rowsWithMissing = CreateRowMetrics(frame)
            .Where(row => row.MissingCount > 0)
            .OrderByDescending(row => row.MissingCount)
            .ThenBy(row => row.Index)
            .ToImmutableArray();
        var distribution = rowsWithMissing
            .GroupBy(row => row.MissingCount)
            .OrderByDescending(group => group.Key)
            .Select(group => new MissingDistribution(group.Key, group.Count()))
            .ToImmutableArray();
        SummaryStatistics summary = CreateSummary(frame, columnMetrics, rowsWithMissing);

        // Perform data validation if validator is provided
        ValidationSummary? validationSummary = null;
        if (_dataValidator != null)
        {
            validationSummary = _dataValidator.ValidateData(loadContext);
            
            // Always use the modified data (which now includes validation messages column)
            if (validationSummary.ModifiedData != null)
            {
                loadContext = new FileLoadContext(
                    loadContext.FilePath, 
                    loadContext.Extension, 
                    loadContext.SizeInBytes, 
                    loadContext.EncodingName, 
                    validationSummary.ModifiedData);
            }
        }

        return new AnalysisResult(loadContext, columnMetrics, rowsWithMissing, distribution, summary, options, validationSummary);
    }

    private static IEnumerable<ColumnMetrics> CreateColumnMetrics(Frame<int, string> frame)
    {
        int totalRows = frame.RowCount;

        foreach (string columnKey in frame.ColumnKeys)
        {
            Series<int, object?> column = frame.GetColumn<object?>(columnKey);
            int nonMissingCount = column.ValueCount;
            int missingCount = totalRows - nonMissingCount;
            double missingPercentage = totalRows == 0 ? 0d : (double)missingCount / totalRows * 100d;
            string dataType = column.Values.Select(value => value?.GetType()?.Name ?? "Unknown").FirstOrDefault("Unknown");
            int uniqueValues = column.Values.Distinct().Count();

            yield return new ColumnMetrics(
                columnKey,
                missingCount,
                missingPercentage,
                uniqueValues,
                dataType);
        }
    }

    private static IEnumerable<RowMetrics> CreateRowMetrics(Frame<int, string> frame)
    {
        string[] columnKeys = frame.ColumnKeys.ToArray();
        int totalColumns = columnKeys.Length;

        foreach (int rowKey in frame.RowKeys)
        {
            IReadOnlyDictionary<string, object?> values = FrameUtilities.CaptureRowValues(frame, rowKey);
            int populatedCount = values.Values.Count(value => value is not null);
            int missingCount = totalColumns - populatedCount;
            double missingPercentage = totalColumns == 0 ? 0d : (double)missingCount / totalColumns * 100d;

            yield return new RowMetrics(rowKey, missingCount, missingPercentage, values);
        }
    }

    private static SummaryStatistics CreateSummary(
        Frame<int, string> frame,
        ImmutableArray<ColumnMetrics> columns,
        ImmutableArray<RowMetrics> rows)
    {
        long totalCells = (long)frame.RowCount * frame.ColumnCount;
        long totalMissing = columns.Sum(c => (long)c.MissingCount);
        double missingPercentage = totalCells == 0 ? 0d : (double)totalMissing / totalCells * 100d;
        double dataQualityScore = totalCells == 0 ? 100d : (double)(totalCells - totalMissing) / totalCells * 100d;
        int columnsWithMissing = columns.Count(c => c.MissingCount > 0);
        int rowsWithMissing = rows.Length;

        return new SummaryStatistics(
            totalCells,
            totalMissing,
            missingPercentage,
            dataQualityScore,
            columnsWithMissing,
            rowsWithMissing);
    }
}
