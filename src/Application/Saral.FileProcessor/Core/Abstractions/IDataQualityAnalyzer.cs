namespace Saral.FileProcessor.Core.Abstractions;

public interface IDataQualityAnalyzer
{
    AnalysisResult Analyze(FileLoadContext loadContext, AnalysisOptions options);
}