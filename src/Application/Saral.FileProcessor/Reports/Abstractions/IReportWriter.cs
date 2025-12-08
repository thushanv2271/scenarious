namespace Saral.FileProcessor.Reports.Abstractions;

public interface IReportWriter
{
    string WriteReport(AnalysisResult result, string? outputPath = null);
}

public interface IReportRenderer
{
    void Render(AnalysisResult result);
}