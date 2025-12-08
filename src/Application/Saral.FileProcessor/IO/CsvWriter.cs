namespace Saral.FileProcessor.IO;

public static class CsvWriter
{
    public static string SaveValidatedData(Frame<int, string> data, string originalFilePath)
    {
        string directory = Path.GetDirectoryName(originalFilePath) ?? Environment.CurrentDirectory;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFilePath);
        string outputPath = Path.Combine(directory, $"{fileNameWithoutExtension}_validated.csv");

        WriteDataToFile(data, outputPath);
        return outputPath;
    }

    public static void UpdateOriginalFile(Frame<int, string> data, string originalFilePath)
    {
        WriteDataToFile(data, originalFilePath);
    }

    private static void WriteDataToFile(Frame<int, string> data, string filePath)
    {
        using var writer = new StringWriter();

        // Write header
        string[] columns = data.ColumnKeys.ToArray();
        writer.WriteLine(string.Join(",", columns.Select(EscapeCsvField)));

        // Write data rows in the correct order (sorted by row key to maintain original order)
        foreach (int rowKey in data.RowKeys.OrderBy(x => x))
        {
            var values = new List<string>();
            foreach (string column in columns)
            {
                Series<int, object?> columnSeries = data.GetColumn<object?>(column);
                OptionalValue<object?> optional = columnSeries.TryGet(rowKey);
                string value = optional.HasValue ? optional.Value?.ToString() ?? string.Empty : string.Empty;
                values.Add(EscapeCsvField(value));
            }
            writer.WriteLine(string.Join(",", values));
        }

        File.WriteAllText(filePath, writer.ToString());
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return string.Empty;
        }

        // If field contains comma, quote, or newline, wrap in quotes and escape internal quotes
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }
}
