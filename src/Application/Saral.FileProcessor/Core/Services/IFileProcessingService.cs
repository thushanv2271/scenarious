namespace Saral.FileProcessor.Core.Services;

public interface IFileProcessingService
{
    Task<AnalysisResult> ProcessFileAsync(string filePath, AnalysisOptions? options = null, CancellationToken cancellationToken = default);
    Task<AnalysisResult> ProcessFileAsync(Stream fileStream, string fileName, AnalysisOptions? options = null, CancellationToken cancellationToken = default);
    
    // NEW: Multi-file processing methods
    Task<MultiFileAnalysisResult> ProcessMultipleFilesAsync(IEnumerable<string> filePaths, AnalysisOptions? options = null, CancellationToken cancellationToken = default);
    Task<MultiFileAnalysisResult> ProcessMultipleFilesAsync(IEnumerable<FileStreamInfo> fileStreams, AnalysisOptions? options = null, CancellationToken cancellationToken = default);
}

// NEW: File stream information record
public readonly record struct FileStreamInfo(Stream Stream, string FileName);