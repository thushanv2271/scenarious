namespace Saral.FileProcessor.Data.Extensions;

/// <summary>
/// Extension methods for integrating database operations with file processing
/// </summary>
public static class FileProcessingExtensions
{
    /// <summary>
    /// Processes multiple files and saves results to database
    /// </summary>
    /// <param name="fileProcessingService">The file processing service</param>
    /// <param name="fileProcessingResultService">The database service</param>
    /// <param name="filePaths">Collection of file paths to process</param>
    /// <param name="sessionId">Unique session identifier for multi-file processing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Multi-file analysis result with database IDs</returns>
    public static async Task<(MultiFileAnalysisResult Result, List<int> DatabaseIds)> ProcessMultipleFilesWithDatabaseAsync(
        this IFileProcessingService fileProcessingService,
        IFileProcessingResultService fileProcessingResultService,
        IEnumerable<string> filePaths,
        string sessionId,
        AnalysisOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Process files using core service
        MultiFileAnalysisResult result = await fileProcessingService.ProcessMultipleFilesAsync(filePaths, options, cancellationToken);

        // Save results to database
        List<int> databaseIds = await fileProcessingResultService.SaveMultiFileProcessingResultAsync(result, sessionId, cancellationToken);
        
        return (result, databaseIds);
    }

    /// <summary>
    /// Processes a single file and saves results to database
    /// </summary>
    /// <param name="fileProcessingService">The file processing service</param>
    /// <param name="fileProcessingResultService">The database service</param>
    /// <param name="filePath">Path to the file to process</param>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="options">Analysis options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analysis result with database ID</returns>
    public static async Task<(AnalysisResult Result, int DatabaseId)> ProcessFileWithDatabaseAsync(
        this IFileProcessingService fileProcessingService,
        IFileProcessingResultService fileProcessingResultService,
        string filePath,
        Guid sessionId,
        AnalysisOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Process file using core service
        AnalysisResult result = await fileProcessingService.ProcessFileAsync(filePath, options, cancellationToken);

        // Save results to database
        string filename = Path.GetFileName(filePath);
        int databaseId = await fileProcessingResultService.SaveFileValidationResultAsync(result, filename, sessionId, cancellationToken);
        
        return (result, databaseId);
    }
}
