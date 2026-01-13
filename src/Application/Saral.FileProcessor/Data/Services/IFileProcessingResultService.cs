namespace Saral.FileProcessor.Data.Services;

public interface IFileProcessingResultService
{
    /// <summary>
    /// Saves the result of processing a single file to the database.
    /// </summary>
    /// <param name="analysisResult">The analysis result</param>
    /// <param name="filename">The filename</param>
    /// <param name="sessionId">The session identifier</param>
    /// <param name="timePeriod">The time period (e.g., "2024-Q1", "2024")</param>
    /// <param name="collectiveImpairmentType">The collective impairment type (PD or LGD)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The database ID of the saved record</returns>
    Task<int> SaveFileValidationResultAsync(
        AnalysisResult analysisResult, 
        string filename, 
        Guid sessionId,
        string? timePeriod = null,
        string? collectiveImpairmentType = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves the results of processing multiple files to the database as individual records.
    /// </summary>
    /// <param name="analysisResult">The multi-file analysis result</param>
    /// <param name="sessionId">The session identifier</param>
    /// <param name="timePeriod">The time period (e.g., "2024-Q1", "2024")</param>
    /// <param name="collectiveImpairmentType">The collective impairment type (PD or LGD)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of database IDs for the saved records</returns>
    Task<List<int>> SaveMultiFileProcessingResultAsync(
        MultiFileAnalysisResult analysisResult, 
        string sessionId,
        string? timePeriod = null,
        string? collectiveImpairmentType = null,
        CancellationToken cancellationToken = default);
    
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
