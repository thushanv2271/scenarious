namespace Saral.FileProcessor.Reports.Services;

public interface IReportGenerationService
{
    Task<string> GenerateJsonReportAsync(AnalysisResult result, string? outputPath = null, CancellationToken cancellationToken = default);
    Task<string> GenerateExcelReportAsync(AnalysisResult result, string? outputPath = null, CancellationToken cancellationToken = default);
    Task<string> GenerateConsoleReportAsync(AnalysisResult result, CancellationToken cancellationToken = default);
    Task<ReportPaths> GenerateAllReportsAsync(AnalysisResult result, string? basePath = null, CancellationToken cancellationToken = default);
}

public readonly record struct ReportPaths(string? JsonPath, string? ExcelPath, string? ConsolePath);