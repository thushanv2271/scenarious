using Application.Abstractions.Configuration;
using Application.Abstractions.Data;
using Application.Models;
using Domain.LGDCalculation;
using Infrastructure.LgdCalculationSteps.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedKernel;
using System.Diagnostics;

namespace Infrastructure.LgdCalculationSteps.Steps;

/// <summary>
/// Step 1 of LGD Calculation: High-performance CSV file processing with bulk database insertion
/// </summary>
internal sealed class Step1LgdFileExtractionAndCalculation
{
    private readonly IAppConfiguration _appConfiguration;
    private readonly IApplicationDbContext _dbContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Step1LgdFileExtractionAndCalculation> _logger;

    public Step1LgdFileExtractionAndCalculation(
        IAppConfiguration appConfiguration,
        IApplicationDbContext dbContext,
        IServiceProvider serviceProvider,
        ILogger<Step1LgdFileExtractionAndCalculation> logger)
    {
        _appConfiguration = appConfiguration;
        _dbContext = dbContext;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Executes Step 1: LGD file extraction and calculation
    /// </summary>
    /// <param name="createdBy">User who initiated the process</param>
    /// <param name="calculationType">Type of calculation to perform (LGD or VC_LGD)</param>
    /// <param name="type">Optional type to determine file path suffix (Yearly=Y, Quarterly=Q, Monthly=M). If not provided, uses full path from appsettings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    public async Task<Result> ExecuteAsync(
        string createdBy,
        LgdCalculationType calculationType = LgdCalculationType.LGD,
        string? type = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("=== Step 1 {CalculationType} Calculation Started ===", calculationType);

        try
        {
            // Determine which tables to truncate and processing logic to use
            if (calculationType == LgdCalculationType.LGD)
            {
                return await ExecuteLgdCalculationAsync(createdBy, type, cancellationToken);
            }
            else
            {
                return await ExecuteVCLgdCalculationAsync(createdBy, type, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during {CalculationType} calculation Step 1 execution", calculationType);
            return Result.Failure(Error.Failure(
                $"{calculationType}Calculation.Step1.ExecutionError",
                $"An error occurred during {calculationType} calculation Step 1: {ex.Message}"));
        }
    }

    /// <summary>
    /// Executes LGD calculation for standard LGD files
    /// </summary>
    /// <param name="createdBy">User who initiated the process</param>
    /// <param name="type">Optional type to determine file path suffix</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    private async Task<Result> ExecuteLgdCalculationAsync(
        string createdBy,
        string? type,
        CancellationToken cancellationToken)
    {
        var totalExecutionStopwatch = Stopwatch.StartNew();
        _logger.LogInformation("=== Step 1 LGD Calculation Started ===");

        try
        {
            // Truncate LGD tables before processing - much faster than DELETE for large datasets
            var dbContext = _dbContext as Microsoft.EntityFrameworkCore.DbContext;
            if (dbContext is not null)
            {
                _logger.LogInformation("Truncating LGD tables before processing...");
                // Use lowercase table names as PostgreSQL converts them to lowercase by default
                await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE lgd_details RESTART IDENTITY CASCADE", cancellationToken);
                await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE lgd_file_details RESTART IDENTITY CASCADE", cancellationToken);
                _logger.LogInformation("LGD tables truncated successfully");
            }

            // Construct file path based on type or use full configuration path
            string lgdFilesPath = LgdFilePathHelper.GetLgdFilesPath(_appConfiguration, type);

            if (!string.IsNullOrWhiteSpace(type))
            {
                _logger.LogInformation("Using LGD files path for type '{Type}': {LGDFilesPath}", type, lgdFilesPath);
            }

            if (!Directory.Exists(lgdFilesPath))
            {
                return Result.Failure(Error.NotFound(
                    "LGDFiles.DirectoryNotFound",
                    $"LGD files directory not found: {lgdFilesPath}"));
            }

            // Get all CSV files in the directory
            string[] csvFiles = Directory.GetFiles(lgdFilesPath, "*.csv");

            if (csvFiles.Length == 0)
            {
                return Result.Failure(Error.NotFound(
                    "LGDFiles.NoFilesFound",
                    $"No CSV files found in directory: {lgdFilesPath}"));
            }

            _logger.LogDebug("Processing {CsvFileCount} CSV files...", csvFiles.Length);

            // ==== STEP 1: LOAD CSV FILE DATA ====
            var loadDataStopwatch = Stopwatch.StartNew();

            List<LgdFileDetails> allLgdFileDetails = new();
            List<LgdDetails> allLgdDetails = new();

            foreach (string filePath in csvFiles)
            {
                Result<(LgdFileDetails lgdFileDetails, List<LgdDetails> lgdDetails)> processingResult =
                    ProcessFileInMemory(filePath, createdBy);

                if (processingResult.IsFailure)
                {
                    return Result.Failure(processingResult.Error);
                }

                allLgdFileDetails.Add(processingResult.Value.lgdFileDetails);
                allLgdDetails.AddRange(processingResult.Value.lgdDetails);
            }

            loadDataStopwatch.Stop();
            _logger.LogInformation("1. Load CSV Data: {ElapsedMs}ms ({ElapsedSec:F2}s) - {RecordCount} records from {FileCount} files",
                loadDataStopwatch.ElapsedMilliseconds, loadDataStopwatch.Elapsed.TotalSeconds, allLgdDetails.Count, allLgdFileDetails.Count);

            // ==== STEP 2: CALCULATE DISCOUNT FACTORS AND DISCOUNTED CASHFLOWS ====
            var calculationStopwatch = Stopwatch.StartNew();
            _logger.LogDebug("2. Starting discount factor and discounted cashflows calculation phase...");

            CalculateDiscountFactors(allLgdDetails);

            calculationStopwatch.Stop();
            _logger.LogDebug("2. Calculate Discount Factors & Discounted Cashflows: {ElapsedMs}ms ({ElapsedSec:F2}s)",
                calculationStopwatch.ElapsedMilliseconds, calculationStopwatch.Elapsed.TotalSeconds);

            // ==== STEP 3: INSERT ALL DATA ====
            var insertDataStopwatch = Stopwatch.StartNew();

            await BulkInsertDataAsync(allLgdFileDetails, allLgdDetails, cancellationToken);

            insertDataStopwatch.Stop();
            _logger.LogDebug("3. Insert Data: {ElapsedMs}ms ({ElapsedSec:F2}s)",
                insertDataStopwatch.ElapsedMilliseconds, insertDataStopwatch.Elapsed.TotalSeconds);

            totalExecutionStopwatch.Stop();
            _logger.LogInformation("=== Step 1 LGD Calculation Completed in {ElapsedMs}ms ({ElapsedSec:F2} seconds) ===",
                totalExecutionStopwatch.ElapsedMilliseconds, totalExecutionStopwatch.Elapsed.TotalSeconds);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during LGD calculation Step 1 execution");
            return Result.Failure(Error.Failure(
                "LGDCalculation.Step1.ExecutionError",
                $"An error occurred during LGD calculation Step 1: {ex.Message}"));
        }
    }

    /// <summary>
    /// Processes a single CSV file and returns the data in memory (no database operations)
    /// </summary>
    /// <param name="filePath">Path to the CSV file</param>
    /// <param name="createdBy">User who initiated the process</param>
    /// <returns>Result containing LgdFileDetails and LgdDetails</returns>
    private static Result<(LgdFileDetails lgdFileDetails, List<LgdDetails> lgdDetails)> ProcessFileInMemory(
        string filePath,
        string createdBy)
    {
        string fileName = Path.GetFileName(filePath);

        // Parse file name to extract metadata
        LgdFileNameInfo? fileNameInfo = LgdFileNameParser.ParseFileName(fileName);
        if (fileNameInfo is null)
        {
            return Result.Failure<(LgdFileDetails, List<LgdDetails>)>(Error.Validation(
                "LGDFiles.InvalidFileName",
                $"Unable to parse file name: {fileName}"));
        }

        // Create file details record with extracted period
        string period = LgdFileNameParser.ExtractPeriod(fileNameInfo);
        var lgdFileDetails = LgdFileDetails.Create(
            fileName,
            fileNameInfo.Year,
            fileNameInfo.Part,
            period,
            createdBy);

        // Read LGD data from CSV file using high-performance reader
        List<LgdDetailsCreationRequest> lgdDataRequests;
        try
        {
            lgdDataRequests = LgdFastCsvReader.ReadLgdDataFromCsv(filePath, lgdFileDetails.Id);
        }
        catch (Exception ex)
        {
            return Result.Failure<(LgdFileDetails, List<LgdDetails>)>(Error.Failure(
                "LGDFiles.CsvReadError",
                $"Error reading CSV file {fileName}: {ex.Message}"));
        }

        if (lgdDataRequests.Count == 0)
        {
            return Result.Failure<(LgdFileDetails, List<LgdDetails>)>(Error.Validation(
                "LGDFiles.NoDataFound",
                $"No LGD data found in file: {fileName}"));
        }

        // Create LGD details entities in memory
        var lgdDetailsList = lgdDataRequests
            .Select(LgdDetails.Create)
            .ToList();

        return Result.Success((lgdFileDetails, lgdDetailsList));
    }

    /// <summary>
    /// Performs high-performance batch insertion with parallel processing and optimized batch sizes
    /// </summary>
    /// <param name="allLgdFileDetails">All LGD file details to insert</param>
    /// <param name="allLgdDetails">All LGD details to insert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    private async Task BulkInsertDataAsync(
        List<LgdFileDetails> allLgdFileDetails,
        List<LgdDetails> allLgdDetails,
        CancellationToken cancellationToken)
    {
        try
        {
            // Insert LGD file details first (smaller dataset, needed for foreign keys)
            _logger.LogDebug("Inserting {LgdFileDetailCount} LGD file details...", allLgdFileDetails.Count);
            _dbContext.LgdFileDetails.AddRange(allLgdFileDetails);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Optimize batch size based on record count and available memory
            int batchSize = CalculateOptimalBatchSize(allLgdDetails.Count);
            _logger.LogDebug("Using batch size: {BatchSize} for {LgdRecordCount} LGD records", batchSize, allLgdDetails.Count);

            // Process LGD details in parallel batches
            await ProcessLgdDetailsInBatchesAsync(allLgdDetails, batchSize, cancellationToken);
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
    /// Processes LGD details in parallel batches using multiple database connections for maximum performance
    /// </summary>
    /// <param name="allLgdDetails">All LGD details to insert</param>
    /// <param name="batchSize">Size of each batch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    private async Task ProcessLgdDetailsInBatchesAsync(
        List<LgdDetails> allLgdDetails,
        int batchSize,
        CancellationToken cancellationToken)
    {
        int totalBatches = (int)Math.Ceiling((double)allLgdDetails.Count / batchSize);
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
            var batch = allLgdDetails.Skip(skip).Take(batchSize).ToList();

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
    /// <param name="batch">Batch of LGD details to insert</param>
    /// <param name="batchNumber">Current batch number for logging</param>
    /// <param name="totalBatches">Total number of batches for logging</param>
    /// <param name="semaphore">Semaphore to limit concurrent operations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    private async Task ProcessBatchInParallel(
        List<LgdDetails> batch,
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
                await dbContext.LgdDetails.AddRangeAsync(batch, cancellationToken);

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
                throw new InvalidOperationException($"Failed to process LGD batch {batchNumber}/{totalBatches} with {batch.Count} records: {e.Message}", e);
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Calculates discount factors and discounted cashflows for all LGD details using Excel formulas
    /// </summary>
    /// <param name="allLgdDetails">All LGD details to process</param>
    private static void CalculateDiscountFactors(List<LgdDetails> allLgdDetails)
    {
        foreach (LgdDetails lgdDetail in allLgdDetails)
        {
            // The discount factor (DCF) and discounted cashflows are already calculated in the CSV reader during parsing
            // This method is here in case we need to recalculate or perform additional processing
            // For now, we'll just ensure the calculations are correct and log any issues

            if (lgdDetail.Dcf == 0 && !string.IsNullOrWhiteSpace(lgdDetail.FacilityNumber) && lgdDetail.FirstNplDate.HasValue)
            {
                // Recalculate DCF if it was set to 0 but shouldn't be
                lgdDetail.Dcf = LgdDiscountFactorCalculator.CalculateDiscountFactor(
                    lgdDetail.FacilityNumber,
                    lgdDetail.ReceiptDate,
                    lgdDetail.FirstNplDate,
                    lgdDetail.InterestRate);

                // Recalculate discounted cashflows with updated DCF
                lgdDetail.DiscountedCashflows = LgdDiscountedCashflowsCalculator.CalculateDiscountedCashflows(
                    lgdDetail.FacilityNumber,
                    lgdDetail.Cashflow,
                    lgdDetail.Dcf);
            }
        }
    }

    /// <summary>
    /// Executes VC LGD calculation for VC LGD files
    /// </summary>
    /// <param name="createdBy">User who initiated the process</param>
    /// <param name="type">Optional type to determine file path suffix</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    private async Task<Result> ExecuteVCLgdCalculationAsync(
        string createdBy,
        string? type,
        CancellationToken cancellationToken)
    {
        var totalExecutionStopwatch = Stopwatch.StartNew();
        _logger.LogInformation("=== Step 1 VC LGD Calculation Started ===");

        try
        {
            // Truncate VC LGD tables before processing - much faster than DELETE for large datasets
            var dbContext = _dbContext as Microsoft.EntityFrameworkCore.DbContext;
            if (dbContext is not null)
            {
                _logger.LogInformation("Truncating VC LGD tables before processing...");
                // Use lowercase table names as PostgreSQL converts them to lowercase by default
                await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE vc_lgd_details RESTART IDENTITY CASCADE", cancellationToken);
                await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE vc_lgd_file_details RESTART IDENTITY CASCADE", cancellationToken);
                _logger.LogInformation("VC LGD tables truncated successfully");
            }

            // Construct file path based on type or use full configuration path
            string vcLgdFilesPath = LgdFilePathHelper.GetVCLgdFilesPath(_appConfiguration, type);

            if (!string.IsNullOrWhiteSpace(type))
            {
                _logger.LogInformation("Using VC LGD files path for type '{Type}': {VCLGDFilesPath}", type, vcLgdFilesPath);
            }

            if (!Directory.Exists(vcLgdFilesPath))
            {
                return Result.Failure(Error.NotFound(
                    "VCLGDFiles.DirectoryNotFound",
                    $"VC LGD files directory not found: {vcLgdFilesPath}"));
            }

            // Get all CSV files in the directory
            string[] csvFiles = Directory.GetFiles(vcLgdFilesPath, "*.csv");

            if (csvFiles.Length == 0)
            {
                return Result.Failure(Error.NotFound(
                    "VCLGDFiles.NoFilesFound",
                    $"No CSV files found in VC LGD directory: {vcLgdFilesPath}"));
            }

            _logger.LogDebug("Processing {CsvFileCount} VC LGD CSV files...", csvFiles.Length);

            // ==== STEP 1: LOAD CSV FILE DATA ====
            var loadDataStopwatch = Stopwatch.StartNew();

            List<VCLgdFileDetails> allVCLgdFileDetails = new();
            List<VCLgdDetails> allVCLgdDetails = new();

            foreach (string filePath in csvFiles)
            {
                Result<(VCLgdFileDetails vcLgdFileDetails, List<VCLgdDetails> vcLgdDetails)> processingResult =
                    ProcessVCLgdFileInMemory(filePath, createdBy);

                if (processingResult.IsFailure)
                {
                    return Result.Failure(processingResult.Error);
                }

                allVCLgdFileDetails.Add(processingResult.Value.vcLgdFileDetails);
                allVCLgdDetails.AddRange(processingResult.Value.vcLgdDetails);
            }

            loadDataStopwatch.Stop();
            _logger.LogInformation("1. Load VC LGD CSV Data: {ElapsedMs}ms ({ElapsedSec:F2}s) - {RecordCount} records from {FileCount} files",
                loadDataStopwatch.ElapsedMilliseconds, loadDataStopwatch.Elapsed.TotalSeconds, allVCLgdDetails.Count, allVCLgdFileDetails.Count);

            // ==== STEP 2: CALCULATE DISCOUNT FACTORS AND DISCOUNTED CASHFLOWS ====
            var calculationStopwatch = Stopwatch.StartNew();
            _logger.LogDebug("2. Starting VC LGD discount factor and discounted cashflows calculation phase...");

            CalculateVCLgdDiscountFactors(allVCLgdDetails);

            calculationStopwatch.Stop();
            _logger.LogDebug("2. Calculate VC LGD Discount Factors & Discounted Cashflows: {ElapsedMs}ms ({ElapsedSec:F2}s)",
                calculationStopwatch.ElapsedMilliseconds, calculationStopwatch.Elapsed.TotalSeconds);

            // ==== STEP 3: INSERT ALL DATA ====
            var insertDataStopwatch = Stopwatch.StartNew();

            await BulkInsertVCLgdDataAsync(allVCLgdFileDetails, allVCLgdDetails, cancellationToken);

            insertDataStopwatch.Stop();
            _logger.LogDebug("3. Insert VC LGD Data: {ElapsedMs}ms ({ElapsedSec:F2}s)",
                insertDataStopwatch.ElapsedMilliseconds, insertDataStopwatch.Elapsed.TotalSeconds);

            totalExecutionStopwatch.Stop();
            _logger.LogInformation("=== Step 1 VC LGD Calculation Completed in {ElapsedMs}ms ({ElapsedSec:F2} seconds) ===",
                totalExecutionStopwatch.ElapsedMilliseconds, totalExecutionStopwatch.Elapsed.TotalSeconds);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during VC LGD calculation Step 1 execution");
            return Result.Failure(Error.Failure(
                "VCLGDCalculation.Step1.ExecutionError",
                $"An error occurred during VC LGD calculation Step 1: {ex.Message}"));
        }
    }

    /// <summary>
    /// Processes a single VC LGD CSV file and returns the data in memory (no database operations)
    /// </summary>
    /// <param name="filePath">Path to the CSV file</param>
    /// <param name="createdBy">User who initiated the process</param>
    /// <returns>Result containing VCLgdFileDetails and VCLgdDetails</returns>
    private static Result<(VCLgdFileDetails vcLgdFileDetails, List<VCLgdDetails> vcLgdDetails)> ProcessVCLgdFileInMemory(
        string filePath,
        string createdBy)
    {
        string fileName = Path.GetFileName(filePath);

        // Parse file name to extract metadata (using same logic as LGD)
        LgdFileNameInfo? fileNameInfo = LgdFileNameParser.ParseFileName(fileName);
        if (fileNameInfo is null)
        {
            return Result.Failure<(VCLgdFileDetails, List<VCLgdDetails>)>(Error.Validation(
                "VCLGDFiles.InvalidFileName",
                $"Unable to parse VC LGD file name: {fileName}"));
        }

        // Create VC LGD file details record with extracted period
        string period = LgdFileNameParser.ExtractPeriod(fileNameInfo);
        var vcLgdFileDetails = VCLgdFileDetails.Create(
            fileName,
            fileNameInfo.Year,
            fileNameInfo.Part,
            period,
            createdBy);

        // Read VC LGD data from CSV file using high-performance reader
        List<VCLgdDetailsCreationRequest> vcLgdDataRequests;
        try
        {
            vcLgdDataRequests = LgdFastCsvReader.ReadVCLgdDataFromCsv(filePath, vcLgdFileDetails.Id);
        }
        catch (Exception ex)
        {
            return Result.Failure<(VCLgdFileDetails, List<VCLgdDetails>)>(Error.Failure(
                "VCLGDFiles.CsvReadError",
                $"Error reading VC LGD CSV file {fileName}: {ex.Message}"));
        }

        if (vcLgdDataRequests.Count == 0)
        {
            return Result.Failure<(VCLgdFileDetails, List<VCLgdDetails>)>(Error.Validation(
                "VCLGDFiles.NoDataFound",
                $"No VC LGD data found in file: {fileName}"));
        }

        // Create VC LGD details entities in memory
        var vcLgdDetailsList = vcLgdDataRequests
            .Select(VCLgdDetails.Create)
            .ToList();

        return Result.Success((vcLgdFileDetails, vcLgdDetailsList));
    }

    /// <summary>
    /// Performs high-performance batch insertion for VC LGD data with parallel processing and optimized batch sizes
    /// </summary>
    /// <param name="allVCLgdFileDetails">All VC LGD file details to insert</param>
    /// <param name="allVCLgdDetails">All VC LGD details to insert</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    private async Task BulkInsertVCLgdDataAsync(
        List<VCLgdFileDetails> allVCLgdFileDetails,
        List<VCLgdDetails> allVCLgdDetails,
        CancellationToken cancellationToken)
    {
        try
        {
            // Insert VC LGD file details first (smaller dataset, needed for foreign keys)
            _logger.LogDebug("Inserting {VCLgdFileDetailCount} VC LGD file details...", allVCLgdFileDetails.Count);
            _dbContext.VCLgdFileDetails.AddRange(allVCLgdFileDetails);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Optimize batch size based on record count and available memory
            int batchSize = CalculateOptimalBatchSize(allVCLgdDetails.Count);
            _logger.LogDebug("Using batch size: {BatchSize} for {VCLgdRecordCount} VC LGD records", batchSize, allVCLgdDetails.Count);

            // Process VC LGD details in parallel batches
            await ProcessVCLgdDetailsInBatchesAsync(allVCLgdDetails, batchSize, cancellationToken);
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
                    detailedError += " (Possible duplicate records - VC LGD files may have been processed before)";
                }
            }

            throw new InvalidOperationException($"Bulk VC LGD database insertion failed: {detailedError}", ex);
        }
    }

    /// <summary>
    /// Processes VC LGD details in parallel batches using multiple database connections for maximum performance
    /// </summary>
    /// <param name="allVCLgdDetails">All VC LGD details to insert</param>
    /// <param name="batchSize">Size of each batch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    private async Task ProcessVCLgdDetailsInBatchesAsync(
        List<VCLgdDetails> allVCLgdDetails,
        int batchSize,
        CancellationToken cancellationToken)
    {
        int totalBatches = (int)Math.Ceiling((double)allVCLgdDetails.Count / batchSize);
        var batchStopwatch = Stopwatch.StartNew();

        // Use half the processor count for parallel connections to avoid overwhelming the database
        int maxConcurrency = Math.Max(1, Environment.ProcessorCount / 2);
        _logger.LogDebug("Processing {TotalBatches} VC LGD batches in parallel using {MaxConcurrency} concurrent connections...", totalBatches, maxConcurrency);

        // Create semaphore to limit concurrent database operations
        using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

        // Create all batch tasks
        var batchTasks = new List<Task>();

        for (int i = 0; i < totalBatches; i++)
        {
            int batchIndex = i; // Capture loop variable for closure
            int skip = batchIndex * batchSize;
            var batch = allVCLgdDetails.Skip(skip).Take(batchSize).ToList();

            Task batchTask = ProcessVCLgdBatchInParallel(
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
        _logger.LogDebug("All {TotalBatches} VC LGD batches completed in parallel in {ElapsedMs}ms", totalBatches, batchStopwatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Processes a single VC LGD batch in parallel using a dedicated database context
    /// </summary>
    /// <param name="batch">Batch of VC LGD details to insert</param>
    /// <param name="batchNumber">Current batch number for logging</param>
    /// <param name="totalBatches">Total number of batches for logging</param>
    /// <param name="semaphore">Semaphore to limit concurrent operations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    private async Task ProcessVCLgdBatchInParallel(
        List<VCLgdDetails> batch,
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
                await dbContext.VCLgdDetails.AddRangeAsync(batch, cancellationToken);

                // Save changes for this batch
                await dbContext.SaveChangesAsync(cancellationToken);

                batchStopwatch.Stop();
                _logger.LogDebug("VC LGD Batch {BatchNumber}/{TotalBatches} completed: {RecordCount} records in {ElapsedMs}ms",
                    batchNumber, totalBatches, batch.Count, batchStopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                batchStopwatch.Stop();
                _logger.LogError(ex, "Failed to process VC LGD batch {BatchNumber}/{TotalBatches} with {RecordCount} records",
                    batchNumber, totalBatches, batch.Count);
                throw new InvalidOperationException($"Failed to process VC LGD batch {batchNumber}/{totalBatches} with {batch.Count} records: {ex.Message}", ex);
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Calculates discount factors and discounted cashflows for all VC LGD details using Excel formulas
    /// </summary>
    /// <param name="allVCLgdDetails">All VC LGD details to process</param>
    private void CalculateVCLgdDiscountFactors(List<VCLgdDetails> allVCLgdDetails)
    {
        foreach (VCLgdDetails vcLgdDetail in allVCLgdDetails)
        {
            // The discount factor (DCF) and discounted cashflows are already calculated in the CSV reader during parsing
            // This method is here in case we need to recalculate or perform additional processing
            // For now, we'll just ensure the calculations are correct and log any issues

            if (vcLgdDetail.Dcf == 0 && !string.IsNullOrWhiteSpace(vcLgdDetail.FacilityNumber) && vcLgdDetail.FirstNplDate.HasValue)
            {
                // Recalculate DCF if it was set to 0 but shouldn't be
                // Similar logic to LGD calculation - could be extracted to a common method if needed
                _logger.LogWarning("VC LGD Detail {FacilityNumber} has DCF = 0 but should be calculated. This may indicate a calculation issue during CSV parsing.",
                    vcLgdDetail.FacilityNumber);
            }
        }
    }
}