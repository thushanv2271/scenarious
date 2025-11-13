using Application.Abstractions.Configuration;
using Application.Abstractions.Data;
using Application.Models;
using Domain.PDCalculation;
using Infrastructure.PDCalculationSteps.Helpers;
using SharedKernel;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.PDCalculationSteps.Steps;

/// <summary>
/// Step 1 of PD Calculation: High-performance CSV file processing with bulk database insertion
/// </summary>
public class Step1FileExtractionAndCalculation
{
    private readonly IAppConfiguration _appConfiguration;
    private readonly IApplicationDbContext _dbContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Step1FileExtractionAndCalculation> _logger;

    public Step1FileExtractionAndCalculation(
        IAppConfiguration appConfiguration,
        IApplicationDbContext dbContext,
        IServiceProvider serviceProvider,
        ILogger<Step1FileExtractionAndCalculation> logger)
    {
        _appConfiguration = appConfiguration;
        _dbContext = dbContext;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Executes Step 1: processes all CSV files in the PD files directory with bulk operations
    /// </summary>
    /// <param name="quarterEndedDates">Dictionary mapping file identifiers to quarter ended dates</param>
    /// <param name="datePassedDueBuckets">List of date passed due bucket configurations</param>
    /// <param name="finalBucketPayload">Final bucket calculation configuration</param>
    /// <param name="createdBy">User who initiated the process</param>
    /// <param name="type">Optional type to determine file path suffix (Yearly=Y, Quarterly=Q, Monthly=M). If not provided, uses full path from appsettings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    public async Task<Result> ExecuteAsync(
        Dictionary<string, DateTime> quarterEndedDates,
        List<DatePassedDueBucket> datePassedDueBuckets,
        FinalBucketPayload finalBucketPayload,
        string createdBy,
        string? type = null,
        CancellationToken cancellationToken = default)
    {
        var totalExecutionStopwatch = Stopwatch.StartNew();
        _logger.LogInformation("=== Step 1 PD Calculation Started ===");

        try
        {
            // Construct file path based on type or use full configuration path
            string pdFilesPath = PDFilePathHelper.GetPDFilesPath(_appConfiguration, type);

            if (!string.IsNullOrWhiteSpace(type))
            {
                _logger.LogInformation("Using PD files path for type '{Type}': {PDFilesPath}", type, pdFilesPath);
            }

            if (!Directory.Exists(pdFilesPath))
            {
                return Result.Failure(Error.NotFound(
                    "PDFiles.DirectoryNotFound",
                    $"PD files directory not found: {pdFilesPath}"));
            }

            // Get all CSV files in the directory
            string[] csvFiles = Directory.GetFiles(pdFilesPath, "*.csv");

            if (csvFiles.Length == 0)
            {
                return Result.Failure(Error.NotFound(
                    "PDFiles.NoFilesFound",
                    $"No CSV files found in directory: {pdFilesPath}"));
            }

            _logger.LogDebug("Processing {CsvFileCount} CSV files...", csvFiles.Length);

            // ==== STEP 1: LOAD CSV FILE DATA ====
            var loadDataStopwatch = Stopwatch.StartNew();

            List<FileDetails> allFileDetails = new();
            List<LoanDetails> allLoanDetails = new();

            foreach (string filePath in csvFiles)
            {
                Result<(FileDetails fileDetails, List<LoanDetails> loanDetails)> processingResult =
                    ProcessFileInMemory(filePath, quarterEndedDates, datePassedDueBuckets, createdBy);

                if (processingResult.IsFailure)
                {
                    return Result.Failure(processingResult.Error);
                }

                allFileDetails.Add(processingResult.Value.fileDetails);
                allLoanDetails.AddRange(processingResult.Value.loanDetails);
            }

            loadDataStopwatch.Stop();
            _logger.LogInformation("1. Load CSV Data: {ElapsedMs}ms ({ElapsedSec:F2}s) - {RecordCount} records from {FileCount} files",
                loadDataStopwatch.ElapsedMilliseconds, loadDataStopwatch.Elapsed.TotalSeconds, allLoanDetails.Count, allFileDetails.Count);

            // ==== STEP 2: CALCULATE REMAINING MATURITY, BUCKET, FINAL BUCKET ====
            var calculationStopwatch = Stopwatch.StartNew();
            _logger.LogDebug("2. Starting calculation phase...");

            CalculateFinalBuckets(allLoanDetails, finalBucketPayload);

            calculationStopwatch.Stop();
            _logger.LogDebug("2. Calculate Maturity/Buckets: {ElapsedMs}ms ({ElapsedSec:F2}s)",
                calculationStopwatch.ElapsedMilliseconds, calculationStopwatch.Elapsed.TotalSeconds);

            // ==== STEP 3: INSERT ALL DATA ====
            var insertDataStopwatch = Stopwatch.StartNew();

            await BulkInsertDataAsync(allFileDetails, allLoanDetails, cancellationToken);

            insertDataStopwatch.Stop();
            _logger.LogDebug("3. Insert Data: {ElapsedMs}ms ({ElapsedSec:F2}s)",
                insertDataStopwatch.ElapsedMilliseconds, insertDataStopwatch.Elapsed.TotalSeconds);
            totalExecutionStopwatch.Stop();
            _logger.LogInformation("=== Step 1 PD Calculation Completed in {ElapsedMs}ms ({ElapsedSec:F2} seconds) ===",
                totalExecutionStopwatch.ElapsedMilliseconds, totalExecutionStopwatch.Elapsed.TotalSeconds);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(Error.Failure(
                "PDCalculation.Step1.UnexpectedError",
                $"An unexpected error occurred during Step 1 execution: {ex.Message}"));
        }
    }

    /// <summary>
    /// Processes a single CSV file and returns the data in memory (no database operations)
    /// </summary>
    /// <param name="filePath">Path to the CSV file</param>
    /// <param name="quarterEndedDates">Quarter ended dates mapping</param>
    /// <param name="datePassedDueBuckets">List of date passed due bucket configurations</param>
    /// <param name="createdBy">User who initiated the process</param>
    /// <returns>Result containing FileDetails and LoanDetails</returns>
    private static Result<(FileDetails fileDetails, List<LoanDetails> loanDetails)> ProcessFileInMemory(
        string filePath,
        Dictionary<string, DateTime> quarterEndedDates,
        List<Application.Models.DatePassedDueBucket> datePassedDueBuckets,
        string createdBy)
    {
        string fileName = Path.GetFileName(filePath);
        // Note: This method is static so cannot use instance logger directly

        // Parse file name to extract metadata
        FileNameInfo? fileNameInfo = FileNameParser.ParseFileName(fileName);
        if (fileNameInfo is null)
        {
            return Result.Failure<(FileDetails, List<LoanDetails>)>(Error.Validation(
                "PDFiles.InvalidFileName",
                $"Unable to parse file name: {fileName}"));
        }

        // Create file identifier for quarter ended date lookup
        string fileIdentifier = FileNameParser.CreateFileIdentifier(fileNameInfo);

        // Get quarter ended date for this file
        if (!quarterEndedDates.TryGetValue(fileIdentifier, out DateTime quarterEndedDate))
        {
            return Result.Failure<(FileDetails, List<LoanDetails>)>(Error.Validation(
                "PDFiles.QuarterEndedDateNotFound",
                $"Quarter ended date not provided for file: {fileIdentifier}"));
        }

        // Ensure quarter ended date is UTC for PostgreSQL
        if (quarterEndedDate.Kind == DateTimeKind.Unspecified)
        {
            quarterEndedDate = DateTime.SpecifyKind(quarterEndedDate, DateTimeKind.Utc);
        }
        else if (quarterEndedDate.Kind == DateTimeKind.Local)
        {
            quarterEndedDate = quarterEndedDate.ToUniversalTime();
        }

        // Create file details record with extracted period
        string period = FileNameParser.ExtractPeriod(fileNameInfo);
        var fileDetails = FileDetails.Create(
            fileName,
            fileNameInfo.Frequency,
            fileNameInfo.Part,
            quarterEndedDate,
            period,
            createdBy);

        // Read loan data from CSV file using high-performance reader
        List<LoanDetailsCreationRequest> loanDataRequests;
        try
        {
            loanDataRequests = FastCsvReader.ReadLoanDataFromCsv(
                filePath,
                fileDetails.Id,
                quarterEndedDate,
                fileNameInfo.Frequency,
                datePassedDueBuckets);
        }
        catch (Exception ex)
        {
            return Result.Failure<(FileDetails, List<LoanDetails>)>(Error.Failure(
                "PDFiles.CsvReadError",
                $"Error reading CSV file {fileName}: {ex.Message}"));
        }

        if (loanDataRequests.Count == 0)
        {
            return Result.Failure<(FileDetails, List<LoanDetails>)>(Error.Validation(
                "PDFiles.NoDataFound",
                $"No loan data found in file: {fileName}"));
        }

        // Create loan details entities in memory
        var loanDetailsList = loanDataRequests
            .Select(LoanDetails.Create)
            .ToList();

        return Result.Success((fileDetails, loanDetailsList));
    }

    /// <summary>
    /// Performs high-performance batch insertion with parallel processing and optimized batch sizes
    /// </summary>
    /// <param name="allFileDetails">All file details to insert</param>
    /// <param name="allLoanDetails">All loan details to insert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    private async Task BulkInsertDataAsync(
        List<FileDetails> allFileDetails,
        List<LoanDetails> allLoanDetails,
        CancellationToken cancellationToken)
    {
        try
        {
            // Insert file details first (smaller dataset, needed for foreign keys)
            _logger.LogDebug("Inserting {FileDetailCount} file details...", allFileDetails.Count);
            _dbContext.FileDetails.AddRange(allFileDetails);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Optimize batch size based on record count and available memory
            int batchSize = CalculateOptimalBatchSize(allLoanDetails.Count);
            _logger.LogDebug("Using batch size: {BatchSize} for {LoanRecordCount} loan records", batchSize, allLoanDetails.Count);

            // Process loan details in parallel batches
            await ProcessLoanDetailsInBatchesAsync(allLoanDetails, batchSize, cancellationToken);
        }
        catch (Exception ex)
        {
            // Get detailed error information
            string detailedError = ex.Message;
            if (ex.InnerException is not null)
            {
                detailedError += $" Inner exception: {ex.InnerException.Message}";

                // Check for specific database constraint violations
                if (ex.InnerException.Message.Contains("duplicate key"))
                {
                    detailedError += " (Possible duplicate records - files may have been processed before)";
                }
            }

            throw new InvalidOperationException($"Bulk database insertion failed: {detailedError}", ex);
        }
    }

    /// <summary>
    /// Calculates optimal batch size based on record count and system resources
    /// </summary>
    /// <param name="totalRecords">Total number of records to process</param>
    /// <returns>Optimal batch size</returns>
    private static int CalculateOptimalBatchSize(int totalRecords)
    {
        // Base batch size recommendations for PostgreSQL
        return totalRecords switch
        {
            <= 10000 => totalRecords, // Small dataset - single batch
            <= 50000 => 5000,         // Medium dataset - 5K per batch
            <= 200000 => 10000,       // Large dataset - 10K per batch  
            <= 500000 => 15000,       // Very large - 15K per batch
            _ => 20000                 // Massive dataset - 20K per batch (PostgreSQL sweet spot)
        };
    }

    /// <summary>
    /// Processes loan details in parallel batches using multiple database connections for maximum performance
    /// </summary>
    /// <param name="allLoanDetails">All loan details to insert</param>
    /// <param name="batchSize">Size of each batch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    private async Task ProcessLoanDetailsInBatchesAsync(
        List<LoanDetails> allLoanDetails,
        int batchSize,
        CancellationToken cancellationToken)
    {
        int totalBatches = (int)Math.Ceiling((double)allLoanDetails.Count / batchSize);
        var batchStopwatch = Stopwatch.StartNew();

        // Use half the processor count for parallel connections to avoid overwhelming the database
        int maxConcurrency = Math.Max(1, Environment.ProcessorCount / 2);
        _logger.LogDebug("Processing {TotalBatches} batches in parallel using {MaxConcurrency} concurrent connections...", totalBatches, maxConcurrency);

        // Create semaphore to limit concurrent database operations
        using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

        // Create all batch tasks
        var batchTasks = new List<Task>();

        for (int i = 0; i < totalBatches; i++)
        {
            int batchIndex = i; // Capture loop variable for closure
            int skip = batchIndex * batchSize;
            var batch = allLoanDetails.Skip(skip).Take(batchSize).ToList();

            Task batchTask = ProcessBatchInParallel(
                batch,
                batchIndex + 1,
                totalBatches,
                semaphore,
                cancellationToken);

            batchTasks.Add(batchTask);
        }

        // Wait for all batches to complete
        await Task.WhenAll(batchTasks);

        batchStopwatch.Stop();
        _logger.LogDebug("All {TotalBatches} batches completed in parallel in {ElapsedMs}ms", totalBatches, batchStopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Processes a single batch in parallel using a dedicated database context
    /// </summary>
    /// <param name="batch">Batch of loan details to insert</param>
    /// <param name="batchNumber">Current batch number for logging</param>
    /// <param name="totalBatches">Total number of batches for logging</param>
    /// <param name="semaphore">Semaphore to limit concurrent operations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    private async Task ProcessBatchInParallel(
        List<LoanDetails> batch,
        int batchNumber,
        int totalBatches,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            // Create a new scope and database context for this batch
            using IServiceScope scope = _serviceProvider.CreateScope();
            IApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            // Configure context for optimal bulk insertion
            if (dbContext is Microsoft.EntityFrameworkCore.DbContext efDbContext)
            {
                efDbContext.ChangeTracker.AutoDetectChangesEnabled = false;
                efDbContext.ChangeTracker.QueryTrackingBehavior = Microsoft.EntityFrameworkCore.QueryTrackingBehavior.NoTracking;
            }

            var batchStopwatch = Stopwatch.StartNew();

            try
            {
                // Add all entities in the batch
                await dbContext.LoanDetails.AddRangeAsync(batch, cancellationToken);

                // Save changes for this batch
                await dbContext.SaveChangesAsync(cancellationToken);

                batchStopwatch.Stop();
                _logger.LogDebug("Batch {BatchNumber}/{TotalBatches} completed: {RecordCount} records in {ElapsedMs}ms",
                    batchNumber, totalBatches, batch.Count, batchStopwatch.ElapsedMilliseconds);
            }
            catch (Exception e)
            {
                batchStopwatch.Stop();
                _logger.LogError(e, "ERROR in batch {BatchNumber}/{TotalBatches}: {ErrorMessage}",
                    batchNumber, totalBatches, e.Message);
                throw new InvalidOperationException($"Failed to process batch {batchNumber}/{totalBatches} with {batch.Count} records", e);
            }
        }
        finally
        {
            semaphore.Release();
        }
    }



    /// <summary>
    /// Calculates final buckets for all loan details using customer grouping (in-memory only)
    /// </summary>
    /// <param name="allLoanDetails">All loan details to process</param>
    /// <param name="finalBucketPayload">Final bucket calculation configuration</param>
    private static void CalculateFinalBuckets(
        List<LoanDetails> allLoanDetails,
        Application.Models.FinalBucketPayload finalBucketPayload)
    {
        try
        {
            // Note: This is a static method so we'll need to pass logger or use a different approach
            // For now, we'll remove the logging from this static method

            // Validate final bucket payload
            if (!finalBucketPayload.IsValid())
            {
                throw new ArgumentException($"Invalid final bucket payload: Type={finalBucketPayload.Type}, Percentage={finalBucketPayload.Percentage}");
            }

            // Phase 1: Calculate Remaining Maturity
            var maturityStopwatch = Stopwatch.StartNew();

            // Calculate remaining maturity for each loan (this is typically part of the helper)
            CalculateRemainingMaturity(allLoanDetails);

            maturityStopwatch.Stop();

            // Phase 2: Identify Buckets
            var bucketStopwatch = Stopwatch.StartNew();

            // Identify buckets (this is typically part of the helper)
            IdentifyBuckets(allLoanDetails);

            bucketStopwatch.Stop();

            // Phase 3: Calculate Final Buckets
            var finalBucketStopwatch = Stopwatch.StartNew();

            // Calculate final buckets using the helper (modifies entities in memory)
            FinalBucketCalculationHelper.CalculateFinalBuckets(allLoanDetails, finalBucketPayload);

            finalBucketStopwatch.Stop();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Final bucket calculation failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Calculates remaining maturity for all loan details
    /// </summary>
    /// <param name="allLoanDetails">All loan details to process</param>
    private static void CalculateRemainingMaturity(List<LoanDetails> allLoanDetails)
    {
        // This method would contain the actual remaining maturity calculation logic
        // For now, it's a placeholder since the actual logic might be in the helper
        foreach (LoanDetails loan in allLoanDetails)
        {
            // Placeholder for actual maturity calculation
            _ = loan;
        }
    }

    /// <summary>
    /// Identifies buckets based on days past due for all loan details  
    /// </summary>
    /// <param name="allLoanDetails">All loan details to process</param>
    private static void IdentifyBuckets(List<LoanDetails> allLoanDetails)
    {
        // This method would contain the actual bucket identification logic
        // For now, it's a placeholder since the actual logic might be in the helper
        foreach (LoanDetails loan in allLoanDetails)
        {
            // Placeholder for actual bucket identification
            _ = loan;
        }
    }
}
