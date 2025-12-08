using System.Text.RegularExpressions;

namespace Saral.FileProcessor.Reports.Writers;

public sealed partial class JsonReportWriter : IReportWriter
{
    [GeneratedRegex(@"[^\w\d]")]
    private static partial Regex FileIdRegex();

    [GeneratedRegex(@"\b(20\d{2})\b")]
    private static partial Regex YearWordRegex();

    [GeneratedRegex(@"(20\d{2})")]
    private static partial Regex YearAnywhereRegex();

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string WriteReport(AnalysisResult result, string? outputPath = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        JsonExportResult jsonResult = TransformToJsonFormat(result);
        string finalOutputPath = GetOutputPath(result.FileContext.FilePath, outputPath);

        string jsonString = JsonSerializer.Serialize(jsonResult, SerializerOptions);
        File.WriteAllText(finalOutputPath, jsonString);

        return finalOutputPath;
    }

    private static JsonExportResult TransformToJsonFormat(AnalysisResult result)
    {
        string fileName = Path.GetFileName(result.FileContext.FilePath);
        string fileId = GenerateFileId(fileName);
        string reportPeriod = ExtractReportPeriod(fileName);
        bool isMissingDataValid = result.Summary.TotalMissing == 0;
        bool isValidationValid = result.ValidationSummary == null || result.ValidationSummary.InvalidRows == 0;
        bool isOverallValid = isMissingDataValid && isValidationValid;

        var fileData = new JsonFileData
        {
            FileId = fileId,
            FileName = fileName,
            TotalRows = result.TotalRows,
            SchemaErrors = 0, // Not currently analyzed
            Nulls = result.Summary.TotalMissing,
            RangeErrors = 0, // Not currently analyzed
            ReferentialErrors = 0, // Not currently analyzed
            Duplicates = 0, // Not currently analyzed
            ValidationErrors = result.ValidationSummary?.InvalidRows ?? 0,
            ValidationSuccessRate = result.ValidationSummary?.ValidationSuccessRate ?? 100.0,
            Status = isOverallValid ? "Valid" : "Invalid"
        };

        var summary = new JsonSummary
        {
            TotalFiles = 1, // Single file analysis
            FilesWithErrors = isOverallValid ? 0 : 1,
            ReadyForUse = isOverallValid ? 1 : 0,
            TotalRowsFromValidFiles = isOverallValid ? result.TotalRows : 0,
            ReportPeriod = reportPeriod
        };

        var pagination = new JsonPagination
        {
            TotalRecords = 1,
            PageNumber = 1,
            PageSize = 10
        };

        return new JsonExportResult
        {
            Summary = summary,
            Data = [fileData],
            Pagination = pagination
        };
    }

    private static string GenerateFileId(string fileName)
    {
        // Generate a simple file ID based on filename
        string nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

        // Remove special characters and replace with underscores
        string fileId = FileIdRegex().Replace(nameWithoutExtension, "_").ToUpperInvariant();

        // Ensure it starts with a letter or underscore
        if (char.IsDigit(fileId[0]))
        {
            fileId = "file_" + fileId;
        }

        return fileId;
    }

    private static string ExtractReportPeriod(string fileName)
    {
        // Try to extract year from filename (e.g., "2024_File_1.csv" -> "2024")
        Match yearMatch = YearWordRegex().Match(fileName);
        if (yearMatch.Success)
        {
            return yearMatch.Groups[1].Value;
        }

        // Try to extract year from anywhere in the filename
        Match yearPattern = YearAnywhereRegex().Match(fileName);
        if (yearPattern.Success)
        {
            return yearPattern.Groups[1].Value;
        }

        // Fallback to current year
        return DateTime.Now.Year.ToString(CultureInfo.InvariantCulture);
    }

    private static string GetOutputPath(string inputFilePath, string? customOutputPath)
    {
        if (!string.IsNullOrWhiteSpace(customOutputPath))
        {
            return customOutputPath;
        }

        // Generate default path: same directory as input file, but with .json extension
        string directory = Path.GetDirectoryName(inputFilePath) ?? Environment.CurrentDirectory;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputFilePath);
        string defaultFileName = $"{fileNameWithoutExtension}_analysis.json";

        return Path.Combine(directory, defaultFileName);
    }
}
