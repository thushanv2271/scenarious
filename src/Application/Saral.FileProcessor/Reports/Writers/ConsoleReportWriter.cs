namespace Saral.FileProcessor.Reports.Writers;

public sealed class ConsoleReportWriter(TextWriter? writer = null) : IReportRenderer
{
    private readonly TextWriter _writer = writer ?? System.Console.Out;

    public void Render(AnalysisResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        RenderHeader(result.FileContext);
        RenderPreview(result.FileContext, result.Options.SampleRowLimit);
        RenderColumnAnalysis(result.Columns);
        RenderRowAnalysis(result);
        
        // Render validation results if available
        if (result.ValidationSummary != null)
        {
            RenderValidationSummary(result.ValidationSummary);
        }
        
        RenderSummary(result);
    }

    private void RenderHeader(FileLoadContext context)
    {
        _writer.WriteLine(new string('=', 80));
        _writer.WriteLine("DATA QUALITY ANALYSIS REPORT");
        _writer.WriteLine(new string('=', 80));
        _writer.WriteLine($"File: {context.FilePath}");
        _writer.WriteLine($"File Size: {context.SizeInBytes / 1024d:F2} KB");
        _writer.WriteLine($"File Type: {context.Extension}");
        if (!string.IsNullOrWhiteSpace(context.EncodingName))
        {
            _writer.WriteLine($"Encoding: {context.EncodingName}");
        }
        _writer.WriteLine($"Total Rows: {context.Data.RowCount:N0}");
        _writer.WriteLine($"Total Columns: {context.Data.ColumnCount:N0}");
        _writer.WriteLine();
    }

    private void RenderPreview(FileLoadContext context, int sampleSize)
    {
        Frame<int, string> frame = context.Data;
        string[] columns = frame.ColumnKeys.ToArray();
        int[] rowKeys = frame.RowKeys.Take(sampleSize).ToArray();

        _writer.WriteLine("FIRST ROWS PREVIEW");
        _writer.WriteLine(new string('-', 40));

        if (rowKeys.Length == 0)
        {
            _writer.WriteLine("(no data)");
            _writer.WriteLine();
            return;
        }

        string[][] rows = [.. rowKeys
            .Select(rowKey => FrameUtilities.CaptureRowValues(frame, rowKey))
            .Select(values => columns
                .Select(column => values.TryGetValue(column, out object? value)
                    ? FormatValue(value)
                    : string.Empty)
                .ToArray())];

        RenderTable(columns, rows);
        _writer.WriteLine();
    }

    private void RenderColumnAnalysis(ImmutableArray<ColumnMetrics> columns)
    {
        _writer.WriteLine("COLUMN-WISE MISSING VALUES ANALYSIS");
        _writer.WriteLine(new string('-', 50));

        if (columns.Length == 0)
        {
            _writer.WriteLine("(no columns detected)");
            _writer.WriteLine();
            return;
        }

        ColumnMetrics[] ordered = [.. columns
            .OrderByDescending(column => column.MissingCount)
            .ThenBy(column => column.Name, StringComparer.OrdinalIgnoreCase)];

        string[][] rows = [.. ordered
            .Select(column => new[]
            {
                column.Name,
                column.MissingCount.ToString("N0", CultureInfo.InvariantCulture),
                column.MissingPercentage.ToString("F2", CultureInfo.InvariantCulture) + "%",
                column.UniqueValues.ToString("N0", CultureInfo.InvariantCulture),
                column.DataType
            })];

        RenderTable(["Column", "Missing Count", "Missing %", "Unique Values", "Data Type"], rows);
        _writer.WriteLine();
    }

    private void RenderRowAnalysis(AnalysisResult result)
    {
        _writer.WriteLine("ROW-WISE MISSING VALUES ANALYSIS");
        _writer.WriteLine(new string('-', 50));

        if (result.RowsWithMissing.Length == 0)
        {
            _writer.WriteLine("No rows with missing values detected.");
            _writer.WriteLine();
            return;
        }

        string[][] limitedRows = [.. result.RowsWithMissing
            .Take(result.Options.SampleRowLimit)
            .Select(row => new[]
            {
                row.Index.ToString("N0", CultureInfo.InvariantCulture),
                row.MissingCount.ToString("N0", CultureInfo.InvariantCulture),
                row.MissingPercentage.ToString("F2", CultureInfo.InvariantCulture) + "%"
            })];

        RenderTable(["Row", "Missing Count", "Missing %"], limitedRows);

        _writer.WriteLine();
        _writer.WriteLine("Missing Distribution (Rows)");
        _writer.WriteLine(new string('-', 40));

        string[][] distributionRows = [.. result.MissingDistribution
            .OrderByDescending(bucket => bucket.MissingCount)
            .Select(bucket => new[]
            {
                bucket.MissingCount.ToString("N0", CultureInfo.InvariantCulture),
                bucket.RowCount.ToString("N0", CultureInfo.InvariantCulture)
            })];

        RenderTable(["Missing Cells", "Row Count"], distributionRows);
        _writer.WriteLine();
    }

    private void RenderValidationSummary(ValidationSummary validationSummary)
    {
        _writer.WriteLine("DATA VALIDATION RESULTS");
        _writer.WriteLine(new string('-', 40));
        _writer.WriteLine($"Total rows validated: {validationSummary.TotalRows:N0}");
        _writer.WriteLine($"Valid rows: {validationSummary.ValidRows:N0}");
        _writer.WriteLine($"Invalid rows: {validationSummary.InvalidRows:N0}");
        _writer.WriteLine($"Validation success rate: {validationSummary.ValidationSuccessRate:F2}%");
        _writer.WriteLine();

        if (validationSummary.InvalidRows > 0)
        {
            _writer.WriteLine("VALIDATION ERRORS (Sample)");
            _writer.WriteLine(new string('-', 30));

            RowValidation[] invalidRows = validationSummary.RowValidations
                .Where(r => !r.IsValid)
                .Take(10) // Show first 10 invalid rows
                .ToArray();

            string[][] rows = [.. invalidRows
                .Select(row => new[]
                {
                    (row.RowIndex + 1).ToString("N0", CultureInfo.InvariantCulture), // Convert to 1-based for display
                    row.ValidationMessage ?? string.Empty
                })];

            RenderTable(["Row #", "Validation Errors"], rows);
            
            if (validationSummary.InvalidRows > 10)
            {
                _writer.WriteLine($"... and {validationSummary.InvalidRows - 10} more invalid rows");
            }
            _writer.WriteLine();
        }
    }

    private void RenderSummary(AnalysisResult result)
    {
        SummaryStatistics summary = result.Summary;

        _writer.WriteLine("SUMMARY STATISTICS");
        _writer.WriteLine(new string('-', 30));
        _writer.WriteLine($"Total cells: {summary.TotalCells:N0}");
        _writer.WriteLine($"Total missing values: {summary.TotalMissing:N0}");
        _writer.WriteLine($"Percentage missing: {summary.MissingPercentage:F2}%");
        _writer.WriteLine($"Data quality score: {summary.DataQualityScore:F2}%");
        _writer.WriteLine($"Columns with missing values: {summary.ColumnsWithMissing:N0}");
        _writer.WriteLine($"Rows with missing values: {summary.RowsWithMissing:N0}");
        _writer.WriteLine();
    }

    private void RenderTable(IReadOnlyList<string> headers, IReadOnlyList<string[]> rows)
    {
        int[] widths = [.. headers.Select((header, index) => Math.Max(header.Length, rows.Select(row => row.ElementAtOrDefault(index)?.Length ?? 0).DefaultIfEmpty(0).Max()))];
        WriteRow(headers, widths);
        WriteSeparator(widths);

        foreach (string[] row in rows)
        {
            WriteRow(row, widths);
        }
    }

    private void WriteRow(IReadOnlyList<string> cells, int[] widths)
    {
        string[] formattedCells = [.. cells.Select((cell, index) => (cell ?? string.Empty).PadRight(widths[index]))];
        string line = string.Join(" | ", formattedCells);
        _writer.WriteLine(line);
    }

    private void WriteSeparator(IReadOnlyList<int> widths)
    {
        string separator = string.Join("-+-", widths.Select(width => new string('-', width)));
        _writer.WriteLine(separator);
    }

    private static string FormatValue(object? value)
        => value switch
        {
            null => string.Empty,
            DateTime dateTime => dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
}
