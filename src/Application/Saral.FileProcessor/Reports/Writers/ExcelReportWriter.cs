namespace Saral.FileProcessor.Reports.Writers;

public sealed class ExcelReportWriter : IReportWriter
{
    public string WriteReport(AnalysisResult result, string? outputPath = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        string targetPath = ResolveOutputPath(result.FileContext.FilePath, outputPath);
        string? directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var workbook = new XLWorkbook();
        WriteColumnAnalysis(workbook.AddWorksheet("Column_Analysis"), result.Columns);
        WriteRowAnalysis(workbook.AddWorksheet("Rows_with_Missing"), result);
        WriteSummary(workbook.AddWorksheet("Summary"), result);
        workbook.SaveAs(targetPath);

        return targetPath;
    }

    private static string ResolveOutputPath(string sourceFile, string? desiredPath)
    {
        if (!string.IsNullOrWhiteSpace(desiredPath))
        {
            return desiredPath;
        }

        string directory = Path.GetDirectoryName(sourceFile) ?? Directory.GetCurrentDirectory();
        string fileName = Path.GetFileNameWithoutExtension(sourceFile);
        return Path.Combine(directory, $"{fileName}_data_quality.xlsx");
    }

    private static void WriteColumnAnalysis(IXLWorksheet worksheet, ImmutableArray<ColumnMetrics> columns)
    {
        string[] headers = new[] { "Column", "Missing Count", "Missing %", "Unique Values", "Data Type" };
        WriteHeaders(worksheet, headers);

        ColumnMetrics[] ordered = [.. columns
            .OrderByDescending(column => column.MissingCount)
            .ThenBy(column => column.Name, StringComparer.OrdinalIgnoreCase)];

        for (int index = 0; index < ordered.Length; index++)
        {
            int row = index + 2;
            ColumnMetrics column = ordered[index];

            worksheet.Cell(row, 1).Value = column.Name;
            worksheet.Cell(row, 2).Value = column.MissingCount;
            worksheet.Cell(row, 3).Value = column.MissingPercentage / 100d;
            worksheet.Cell(row, 3).Style.NumberFormat.Format = "0.00%";
            worksheet.Cell(row, 4).Value = column.UniqueValues;
            worksheet.Cell(row, 5).Value = column.DataType;
        }

        AutoFit(worksheet, headers.Length);
    }

    private static void WriteRowAnalysis(IXLWorksheet worksheet, AnalysisResult result)
    {
        string[] columns = result.FileContext.Data.ColumnKeys.ToArray();
        var headers = new List<string> { "Row", "Missing Count", "Missing %" };
        headers.AddRange(columns);
        WriteHeaders(worksheet, headers);

        int limit = Math.Min(result.Options.ExcelRowLimit, result.RowsWithMissing.Length);
        for (int index = 0; index < limit; index++)
        {
            RowMetrics rowMetrics = result.RowsWithMissing[index];
            int excelRow = index + 2;

            worksheet.Cell(excelRow, 1).Value = rowMetrics.Index;
            worksheet.Cell(excelRow, 2).Value = rowMetrics.MissingCount;
            worksheet.Cell(excelRow, 3).Value = rowMetrics.MissingPercentage / 100d;
            worksheet.Cell(excelRow, 3).Style.NumberFormat.Format = "0.00%";

            for (int columnIndex = 0; columnIndex < columns.Length; columnIndex++)
            {
                string columnName = columns[columnIndex];
                rowMetrics.Values.TryGetValue(columnName, out object? value);
                IXLCell cell = worksheet.Cell(excelRow, columnIndex + 4);
                SetCellValue(cell, value);
            }
        }

        AutoFit(worksheet, headers.Count);
    }

    private static void WriteSummary(IXLWorksheet worksheet, AnalysisResult result)
    {
        SummaryStatistics summary = result.Summary;
        WriteHeaders(worksheet, ["Metric", "Value"]);

        var metrics = new (string Label, object Value)[]
        {
            ("Total Rows", result.TotalRows),
            ("Total Columns", result.TotalColumns),
            ("Total Cells", summary.TotalCells),
            ("Total Missing Values", summary.TotalMissing),
            ("Percentage Missing", summary.MissingPercentage / 100d),
            ("Data Quality Score", summary.DataQualityScore / 100d),
            ("Columns With Missing", summary.ColumnsWithMissing),
            ("Rows With Missing", summary.RowsWithMissing)
        };

        for (int index = 0; index < metrics.Length; index++)
        {
            (string? label, object? value) = metrics[index];
            int row = index + 2;
            IXLCell cell = worksheet.Cell(row, 2);
            worksheet.Cell(row, 1).Value = label;

            bool isPercentage = label.Contains("Percentage", StringComparison.OrdinalIgnoreCase) || label.Contains("Score", StringComparison.OrdinalIgnoreCase);

            switch (value)
            {
                case double doubleValue when isPercentage:
                    cell.Value = doubleValue;
                    cell.Style.NumberFormat.Format = "0.00%";
                    break;
                case double doubleValue:
                    cell.Value = doubleValue;
                    break;
                case int intValue:
                    cell.Value = intValue;
                    break;
                case long longValue:
                    cell.Value = longValue;
                    break;
                default:
                    SetCellValue(cell, value);
                    break;
            }
        }

        int startRow = metrics.Length + 3;
        worksheet.Cell(startRow, 1).Value = "Missing Distribution";
        worksheet.Cell(startRow, 1).Style.Font.Bold = true;
        WriteHeaders(worksheet, ["Missing Cells", "Row Count"], startRow + 1);
        MissingDistribution[] distributionRows = [.. result.MissingDistribution.OrderByDescending(bucket => bucket.MissingCount)];

        for (int index = 0; index < distributionRows.Length; index++)
        {
            int row = startRow + 2 + index;
            MissingDistribution bucket = distributionRows[index];
            worksheet.Cell(row, 1).Value = bucket.MissingCount;
            worksheet.Cell(row, 2).Value = bucket.RowCount;
        }

        AutoFit(worksheet, 2);
    }

    private static void WriteHeaders(IXLWorksheet worksheet, IReadOnlyList<string> headers, int startRow = 1)
    {
        int row = startRow;
        for (int index = 0; index < headers.Count; index++)
        {
            IXLCell cell = worksheet.Cell(row, index + 1);
            cell.Value = headers[index];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromTheme(XLThemeColor.Accent1, 0.8);
        }
    }

    private static void AutoFit(IXLWorksheet worksheet, int columnCount)
    {
        for (int column = 1; column <= columnCount; column++)
        {
            worksheet.Column(column).AdjustToContents();
        }
    }

    private static void SetCellValue(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                cell.Clear();
                break;
            case string text:
                cell.Value = text;
                break;
            case DateTime dateTime:
                cell.Value = dateTime;
                break;
            case bool boolean:
                cell.Value = boolean;
                break;
            case byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
                cell.Value = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                break;
            default:
                cell.Value = value.ToString() ?? string.Empty;
                break;
        }
    }
}
