namespace Saral.FileProcessor.Data.Services;

public interface IFileProcessingResultService
{
    /// <summary>
    /// Saves the result of processing a single file to the database.
    /// </summary>
    Task<int> SaveFileValidationResultAsync(AnalysisResult analysisResult, string filename, Guid sessionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves the results of processing multiple files to the database as individual records.
    /// </summary>
    Task<List<int>> SaveMultiFileProcessingResultAsync(MultiFileAnalysisResult analysisResult, string sessionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves file validation results by filename pattern.
    /// </summary>
    Task<IEnumerable<FileValidationResult>> GetFileValidationResultsAsync(string filenamePattern, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves file validation results by session ID.
    /// </summary>
    Task<IEnumerable<FileValidationResult>> GetSessionResultsAsync(string sessionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves recent file processing results.
    /// </summary>
    Task<IEnumerable<FileValidationResult>> GetRecentFileValidationResultsAsync(int count = 50, CancellationToken cancellationToken = default);
}