namespace Saral.FileProcessor.Reports.Services;

public sealed class ReportGenerationService : IReportGenerationService
{
    public async Task<string> GenerateJsonReportAsync(AnalysisResult result, string? outputPath = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var jsonWriter = new JsonReportWriter();
            return jsonWriter.WriteReport(result, outputPath);
        }, cancellationToken);
    }

    public async Task<string> GenerateExcelReportAsync(AnalysisResult result, string? outputPath = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var excelWriter = new ExcelReportWriter();
            return excelWriter.WriteReport(result, outputPath);
        }, cancellationToken);
    }

    public async Task<string> GenerateConsoleReportAsync(AnalysisResult result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var consoleWriter = new ConsoleReportWriter();
            consoleWriter.Render(result);
            return "Console report generated successfully";
        }, cancellationToken);
    }

    public async Task<ReportPaths> GenerateAllReportsAsync(AnalysisResult result, string? basePath = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        // Generate reports in parallel for better performance
        Task<string> jsonTask = GenerateJsonReportAsync(result, basePath, cancellationToken);
        Task<string> excelTask = GenerateExcelReportAsync(result, basePath, cancellationToken);
        Task<string> consoleTask = GenerateConsoleReportAsync(result, cancellationToken);

        string[] results = await Task.WhenAll(jsonTask, excelTask, consoleTask);
        
        return new ReportPaths(
            JsonPath: results[0],
            ExcelPath: results[1], 
            ConsolePath: results[2]);
    }
}
