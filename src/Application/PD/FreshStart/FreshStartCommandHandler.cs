using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Files.UploadFile;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Application.PD.FreshStart;

/// <summary>
/// Handler for the FreshStartCommand that truncates database tables and clears file storage
/// </summary>
internal sealed class FreshStartCommandHandler : ICommandHandler<FreshStartCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<FreshStartCommandHandler> _logger;
    private readonly FileStorageOptions _fileStorageOptions;

    public FreshStartCommandHandler(
        IApplicationDbContext dbContext,
        ILogger<FreshStartCommandHandler> logger,
        IOptions<FileStorageOptions> fileStorageOptions)
    {
        _dbContext = dbContext;
        _logger = logger;
        _fileStorageOptions = fileStorageOptions.Value;
    }

    public async Task<Result> Handle(FreshStartCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CreatedBy))
        {
            return Result.Failure(Error.Validation(
                "FreshStart.InvalidInput",
                "Created by cannot be null or empty"));
        }

        _logger.LogInformation("Fresh Start operation initiated by user: {CreatedBy}", request.CreatedBy);

        var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Truncate database tables
            Result dbResult = await TruncateDatabaseTablesAsync(cancellationToken);
            if (dbResult.IsFailure)
            {
                return dbResult;
            }

            // Clear file storage
            Result fileResult = ClearFileStorage();
            if (fileResult.IsFailure)
            {
                return fileResult;
            }

            totalStopwatch.Stop();
            _logger.LogInformation("Fresh Start operation completed successfully in {ElapsedMs}ms by user: {CreatedBy}",
                totalStopwatch.ElapsedMilliseconds, request.CreatedBy);

            return Result.Success();
        }
        catch (Exception ex)
        {
            totalStopwatch.Stop();
            _logger.LogError(ex, "Fresh Start operation failed after {ElapsedMs}ms for user: {CreatedBy}",
                totalStopwatch.ElapsedMilliseconds, request.CreatedBy);

            return Result.Failure(Error.Failure(
                "FreshStart.OperationFailed",
                $"Fresh Start operation failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Truncates the specified database tables
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the truncation operation</returns>
    private async Task<Result> TruncateDatabaseTablesAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting database tables truncation");
            var dbStopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Check table counts before truncation
            int uploadedFilesCount = await _dbContext.UploadedFiles.CountAsync(cancellationToken);
            int fileValidationResultsCount = await _dbContext.FileValidationResults.CountAsync(cancellationToken);
            int collectiveImpairmentConfigsCount = await _dbContext.CollectiveImpairmentConfigs.CountAsync(cancellationToken);
            int pdAlgorithmResultsCount = await _dbContext.PDAlgorithmResults.CountAsync(cancellationToken);
            int pdProgressTrackingCount = await _dbContext.PDProgressTrackings.CountAsync(cancellationToken);

            int totalRecords = uploadedFilesCount + fileValidationResultsCount +
                              collectiveImpairmentConfigsCount + pdAlgorithmResultsCount + pdProgressTrackingCount;

            if (totalRecords > 0)
            {
                _logger.LogInformation(
                    "Truncating tables - UploadedFiles: {UploadedFilesCount}, " +
                    "FileValidationResults: {FileValidationResultsCount}, " +
                    "CollectiveImpairmentConfigs: {CollectiveImpairmentConfigsCount}, " +
                    "PDAlgorithmResults: {PDAlgorithmResultsCount}, " +
                    "PDProgressTracking: {PDProgressTrackingCount}",
                    uploadedFilesCount, fileValidationResultsCount,
                    collectiveImpairmentConfigsCount, pdAlgorithmResultsCount, pdProgressTrackingCount);

                // Get the underlying DbContext to access Database property
                var dbContext = _dbContext as DbContext;
                if (dbContext is not null)
                {
                    // Truncate tables - use lowercase table names as PostgreSQL converts them to lowercase by default
                    await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE uploaded_files RESTART IDENTITY CASCADE", cancellationToken);
                    await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE file_validation_results RESTART IDENTITY CASCADE", cancellationToken);
                    await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE collective_impairment_configs RESTART IDENTITY CASCADE", cancellationToken);
                    await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE pd_algorithm_results RESTART IDENTITY CASCADE", cancellationToken);
                    await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE pd_progress_tracking RESTART IDENTITY CASCADE", cancellationToken);
                }
                else
                {
                    // Fallback to regular delete if cast fails
                    _logger.LogWarning("Could not cast to DbContext, falling back to regular delete operations");

                    List<Domain.Files.UploadedFile> uploadedFilesToDelete = await _dbContext.UploadedFiles.ToListAsync(cancellationToken);
                    List<Domain.Files.FileValidationResult> fileValidationResultsToDelete = await _dbContext.FileValidationResults.ToListAsync(cancellationToken);
                    List<Domain.CollectiveImpairment.CollectiveImpairmentConfig> collectiveImpairmentConfigsToDelete = await _dbContext.CollectiveImpairmentConfigs.ToListAsync(cancellationToken);
                    List<Domain.PDAlgorithmResults.PDAlgorithmResult> pdAlgorithmResultsToDelete = await _dbContext.PDAlgorithmResults.ToListAsync(cancellationToken);
                    List<Domain.PDProgressTrackings.PDProgressTracking> pdProgressTrackingToDelete = await _dbContext.PDProgressTrackings.ToListAsync(cancellationToken);

                    _dbContext.UploadedFiles.RemoveRange(uploadedFilesToDelete);
                    _dbContext.FileValidationResults.RemoveRange(fileValidationResultsToDelete);
                    _dbContext.CollectiveImpairmentConfigs.RemoveRange(collectiveImpairmentConfigsToDelete);
                    _dbContext.PDAlgorithmResults.RemoveRange(pdAlgorithmResultsToDelete);
                    _dbContext.PDProgressTrackings.RemoveRange(pdProgressTrackingToDelete);

                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }
            else
            {
                _logger.LogInformation("All tables are already empty - no truncation needed");
            }

            dbStopwatch.Stop();
            _logger.LogInformation("Database truncation completed in {ElapsedMs}ms - Cleared {TotalRecords} total records",
                dbStopwatch.ElapsedMilliseconds, totalRecords);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during database tables truncation");
            return Result.Failure(Error.Failure(
                "FreshStart.DatabaseTruncationFailed",
                $"Failed to truncate database tables: {ex.Message}"));
        }
    }

    /// <summary>
    /// Clears the file storage directory
    /// </summary>
    /// <returns>Result of the file clearing operation</returns>
    private Result ClearFileStorage()
    {
        try
        {
            _logger.LogInformation("Starting file storage cleanup");
            var fileStopwatch = System.Diagnostics.Stopwatch.StartNew();

            string rootPath = _fileStorageOptions.RootPath;

            if (string.IsNullOrWhiteSpace(rootPath))
            {
                _logger.LogWarning("File storage root path is not configured - skipping file cleanup");
                return Result.Success();
            }

            if (!Directory.Exists(rootPath))
            {
                _logger.LogInformation("File storage directory does not exist: {RootPath} - nothing to clear", rootPath);
                return Result.Success();
            }

            // Get all files and directories in the root path
            string[] directories = Directory.GetDirectories(rootPath);
            string[] files = Directory.GetFiles(rootPath);

            int totalDirectories = directories.Length;
            int totalFiles = files.Length;

            if (totalDirectories == 0 && totalFiles == 0)
            {
                _logger.LogInformation("File storage directory is already empty: {RootPath}", rootPath);
                fileStopwatch.Stop();
                return Result.Success();
            }

            _logger.LogInformation("Clearing file storage: {RootPath} - Found {DirectoryCount} directories and {FileCount} files",
                rootPath, totalDirectories, totalFiles);

            // Delete all directories
            foreach (string directory in directories)
            {
                try
                {
                    Directory.Delete(directory, true);
                    _logger.LogDebug("Deleted directory: {Directory}", directory);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete directory: {Directory}", directory);
                }
            }

            // Delete all files
            foreach (string file in files)
            {
                try
                {
                    File.Delete(file);
                    _logger.LogDebug("Deleted file: {File}", file);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete file: {File}", file);
                }
            }

            fileStopwatch.Stop();
            _logger.LogInformation("File storage cleanup completed in {ElapsedMs}ms - Cleared {DirectoryCount} directories and {FileCount} files",
                fileStopwatch.ElapsedMilliseconds, totalDirectories, totalFiles);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during file storage cleanup");
            return Result.Failure(Error.Failure(
                "FreshStart.FileStorageCleanupFailed",
                $"Failed to clear file storage: {ex.Message}"));
        }
    }
}