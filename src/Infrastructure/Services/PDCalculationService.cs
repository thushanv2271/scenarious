using Application.Abstractions.Calculations;
using Application.Abstractions.Configuration;
using Application.Abstractions.Data;
using Application.Models;
using Application.DTOs.PD;
using Application.Services;
using Infrastructure.PDCalculationSteps.Steps;
using Infrastructure.PDCalculationSteps.Steps.Summary;
using Microsoft.Extensions.DependencyInjection;
using AppMigrationMatrix = Application.DTOs.PD.MigrationMatrix;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;
using Domain.PDCalculation;

namespace Infrastructure.Services;

/// <summary>
/// Implementation of the PD Calculation service
/// </summary>
public class PDCalculationService : IPDCalculationService
{
    private readonly IAppConfiguration _appConfiguration;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceProvider _serviceProvider;

    public PDCalculationService(
        IAppConfiguration appConfiguration,
        IApplicationDbContext dbContext,
        ILoggerFactory loggerFactory,
        IServiceProvider serviceProvider)
    {
        _appConfiguration = appConfiguration;
        _dbContext = dbContext;
        _loggerFactory = loggerFactory;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Executes step 1 of PD calculation: file extraction, calculation, and DB insertion
    /// </summary>
    /// <param name="quarterEndedDates">Dictionary of file identifiers to quarter ended dates</param>
    /// <param name="datePassedDueBuckets">List of date passed due bucket configurations</param>
    /// <param name="finalBucketPayload">Final bucket calculation configuration</param>
    /// <param name="createdBy">User who initiated the calculation</param>
    /// <param name="type">Optional type to determine file path suffix (Yearly=Y, Quarterly=Q, Monthly=M). If not provided, uses full path from appsettings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    public async Task<Result> ExecuteStep1Async(
        Dictionary<string, DateTime> quarterEndedDates,
        List<DatePassedDueBucket> datePassedDueBuckets,
        FinalBucketPayload finalBucketPayload,
        string createdBy,
        string? type = null,
        CancellationToken cancellationToken = default)
    {
        if (quarterEndedDates == null || quarterEndedDates.Count == 0)
        {
            return Result.Failure(Error.Validation(
                "PDCalculation.InvalidInput",
                "Quarter ended dates dictionary cannot be null or empty"));
        }

        if (datePassedDueBuckets == null || datePassedDueBuckets.Count == 0)
        {
            return Result.Failure(Error.Validation(
                "PDCalculation.InvalidInput",
                "Date passed due buckets list cannot be null or empty"));
        }

        if (finalBucketPayload == null || !finalBucketPayload.IsValid())
        {
            return Result.Failure(Error.Validation(
                "PDCalculation.InvalidInput",
                "Final bucket payload is null or invalid"));
        }

        if (string.IsNullOrWhiteSpace(createdBy))
        {
            return Result.Failure(Error.Validation(
                "PDCalculation.InvalidInput",
                "Created by cannot be null or empty"));
        }

        // Create logger for cleanup operations
        ILogger logger = _loggerFactory.CreateLogger<PDCalculationService>();

        // Delete existing records from LoanDetails and FileDetails tables before starting Step 1
        try
        {
            logger.LogInformation("Starting cleanup: Truncating LoanDetails and FileDetails tables");

            var deleteStopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Check if tables have data before truncating
            int loanDetailsCount = await _dbContext.LoanDetails.CountAsync(cancellationToken);
            int fileDetailsCount = await _dbContext.FileDetails.CountAsync(cancellationToken);

            if (loanDetailsCount > 0 || fileDetailsCount > 0)
            {
                logger.LogInformation("Truncating tables - LoanDetails: {LoanDetailsCount} records, FileDetails: {FileDetailsCount} records",
                    loanDetailsCount, fileDetailsCount);

                // Get the underlying DbContext to access Database property
                var dbContext = _dbContext as DbContext;
                if (dbContext is not null)
                {
                    // Truncate both tables - much faster than DELETE for large datasets
                    // Use lowercase table names as PostgreSQL converts them to lowercase by default
                    await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE loan_details RESTART IDENTITY CASCADE", cancellationToken);
                    await dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE file_details RESTART IDENTITY CASCADE", cancellationToken);
                }
                else
                {
                    // Fallback to regular delete if cast fails
                    logger.LogWarning("Could not cast to DbContext, falling back to regular delete operations");
                    List<LoanDetails> loanDetailsToDelete = await _dbContext.LoanDetails.ToListAsync(cancellationToken);
                    List<FileDetails> fileDetailsToDelete = await _dbContext.FileDetails.ToListAsync(cancellationToken);
                    _dbContext.LoanDetails.RemoveRange(loanDetailsToDelete);
                    _dbContext.FileDetails.RemoveRange(fileDetailsToDelete);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }
            else
            {
                logger.LogInformation("Tables are already empty - no truncation needed");
            }

            deleteStopwatch.Stop();
            logger.LogInformation("Cleanup completed in {ElapsedMs}ms - Cleared {LoanDetailsCount} LoanDetails and {FileDetailsCount} FileDetails records",
                deleteStopwatch.ElapsedMilliseconds, loanDetailsCount, fileDetailsCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during table cleanup");
            return Result.Failure(Error.Failure(
                "PDCalculation.CleanupFailed",
                $"Failed to clean up existing data: {ex.Message}"));
        }

        // Create the appropriate logger for Step1FileExtractionAndCalculation
        ILogger<Step1FileExtractionAndCalculation> step1Logger = _loggerFactory.CreateLogger<Step1FileExtractionAndCalculation>();

        Step1FileExtractionAndCalculation step1 = new(_appConfiguration, _dbContext, _serviceProvider, step1Logger);
        return await step1.ExecuteAsync(quarterEndedDates, datePassedDueBuckets, finalBucketPayload, createdBy, type, cancellationToken);
    }

    /// <summary>
    /// Executes step 2 of PD calculation: migration matrix generation with counts and percentages
    /// </summary>
    /// <param name="createdBy">User who initiated the calculation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the generated migration matrices</returns>
    public async Task<Result<IReadOnlyList<PeriodMigrationMatrix>>> ExecuteStep2Async(
        TimeConfig timeConfig,
        List<DatePassedDueBucket> datePassedDueBuckets,
        List<PDConfiguration> pdConfiguration,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(createdBy))
        {
            return Result.Failure<IReadOnlyList<PeriodMigrationMatrix>>(Error.Validation(
                "PDCalculation.InvalidInput",
                "Created by cannot be null or empty"));
        }

        // Create the appropriate logger for Step2GenerateMigrationMatrices
        ILogger<Step2GenerateMigrationMatrices> step2Logger = _loggerFactory.CreateLogger<Step2GenerateMigrationMatrices>();

        // Get the PDSetupConfigurationService from the service provider
        IPDSetupConfigurationService pdSetupConfigurationService = _serviceProvider.GetRequiredService<IPDSetupConfigurationService>();

        Step2GenerateMigrationMatrices step2 = new(_dbContext, pdSetupConfigurationService, step2Logger, timeConfig, datePassedDueBuckets, pdConfiguration);
        return await step2.ExecuteAsync(createdBy, cancellationToken);
    }

    /// <summary>
    /// Executes step 3 of PD calculation: Generate PD Summary tables from migration matrices
    /// </summary>
    /// <param name="migrationData">Migration matrices response object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing PD summary table JSON</returns>
    public async Task<Result<string>> ExecuteStep3Async(
        Application.Models.PDSummary.MigrationMatricesAndSummaryResponse migrationData,
        CancellationToken cancellationToken = default)
    {
        var totalStepStopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Create logger for Step 3 debugging
        ILogger logger = _loggerFactory.CreateLogger("Step3PDAvgSummary");

        logger.LogInformation("=== Step 3 PD Calculation Started ===");

        if (migrationData is null)
        {
            logger.LogError("Migration data is null");
            return Result.Failure<string>(Error.Validation(
                "PDCalculation.InvalidInput",
                "Migration data cannot be null"));
        }

        logger.LogDebug("Migration data validation: Success={Success}, MatricesCount={MatricesCount}",
            migrationData.Success,
            migrationData.Matrices?.Count ?? 0);

        // ==== STEP 3.1: FETCH LOAN DETAILS WITH PARALLEL PROCESSING ====
        var fetchStopwatch = System.Diagnostics.Stopwatch.StartNew();
        logger.LogInformation("3.1 Starting high-performance loan details fetching...");

        List<Domain.PDCalculation.LoanDetails> loanDetails = await FetchLoanDetailsInBatchesAsync(logger, cancellationToken);

        fetchStopwatch.Stop();
        logger.LogInformation("3.1 Fetch Loan Details: {ElapsedMs}ms ({ElapsedSec:F2}s) - Retrieved {RecordCount} records",
            fetchStopwatch.ElapsedMilliseconds, fetchStopwatch.Elapsed.TotalSeconds, loanDetails.Count);

        // ==== STEP 3.2: GENERATE PD SUMMARY TABLE ====
        var processingStopwatch = System.Diagnostics.Stopwatch.StartNew();
        logger.LogDebug("3.2 Starting PD Summary table generation from migration matrices...");

        Result<string> result = Step3PDAvgSummary.GeneratePDSummaryTable(migrationData, loanDetails, logger, cancellationToken);

        processingStopwatch.Stop();
        logger.LogInformation("3.2 Generate PD Summary: {ElapsedMs}ms ({ElapsedSec:F2}s)",
            processingStopwatch.ElapsedMilliseconds, processingStopwatch.Elapsed.TotalSeconds);

        totalStepStopwatch.Stop();

        if (result.IsSuccess)
        {
            logger.LogInformation("=== Step 3 PD Calculation Completed in {ElapsedMs}ms ({ElapsedSec:F2} seconds) ===",
                totalStepStopwatch.ElapsedMilliseconds, totalStepStopwatch.Elapsed.TotalSeconds);
        }
        else
        {
            logger.LogError("Step 3 failed after {ElapsedMs}ms: {Error}",
                totalStepStopwatch.ElapsedMilliseconds, result.Error.Description);
        }

        return result;
    }

    /// <summary>
    /// Executes step 4 of PD calculation: Generate PD extrapolation tables for all three methods
    /// </summary>
    /// <param name="createdBy">User who initiated the calculation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the PD extrapolation results with all three methods</returns>
    public async Task<Result<PdExtrapolationResultDto>> ExecuteStep4Async(
        AveragePDTablesResponse? step3Data,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(createdBy))
        {
            return Result.Failure<PdExtrapolationResultDto>(Error.Validation(
                "PDCalculation.InvalidInput",
                "Created by cannot be null or empty"));
        }

        // Create the appropriate logger for Step4GenerateExtrapolationTables
        ILogger<Step4GenerateExtrapolationTables> step4Logger = _loggerFactory.CreateLogger<Step4GenerateExtrapolationTables>();

        // Get the PDSetupConfigurationService from the service provider
        IPDSetupConfigurationService pdSetupConfigurationService = _serviceProvider.GetRequiredService<IPDSetupConfigurationService>();

        Step4GenerateExtrapolationTables step4 = new(_dbContext, pdSetupConfigurationService, step4Logger);
        return await step4.ExecuteAsync(step3Data, createdBy, cancellationToken);
    }

    /// <summary>
    /// Fetches loan details using parallel batch processing for improved performance
    /// </summary>
    /// <param name="logger">Logger for debugging and performance tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of loan details</returns>
    private async Task<List<Domain.PDCalculation.LoanDetails>> FetchLoanDetailsInBatchesAsync(
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // First, get the total count to determine optimal batch strategy
            var countStopwatch = System.Diagnostics.Stopwatch.StartNew();
            int totalCount = await _dbContext.LoanDetails.CountAsync(cancellationToken);
            countStopwatch.Stop();

            logger.LogDebug("Count query completed in {ElapsedMs}ms - Found {TotalRecords} total loan records",
                countStopwatch.ElapsedMilliseconds, totalCount);

            if (totalCount == 0)
            {
                logger.LogWarning("No loan details found in database");
                return new List<Domain.PDCalculation.LoanDetails>();
            }

            // For small datasets, use single query
            if (totalCount <= 10000)
            {
                logger.LogDebug("Using single query for small dataset ({TotalCount} records)", totalCount);
                var singleQueryStopwatch = System.Diagnostics.Stopwatch.StartNew();
                List<Domain.PDCalculation.LoanDetails> result = await _dbContext.LoanDetails
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
                singleQueryStopwatch.Stop();

                logger.LogDebug("Single query completed in {ElapsedMs}ms", singleQueryStopwatch.ElapsedMilliseconds);
                return result;
            }

            // For large datasets, use parallel batch processing
            int batchSize = CalculateOptimalBatchSizeForFetch(totalCount);
            int totalBatches = (int)Math.Ceiling((double)totalCount / batchSize);

            logger.LogDebug("Using parallel batch processing: {TotalRecords} records, {BatchSize} per batch, {TotalBatches} batches",
                totalCount, batchSize, totalBatches);

            // Use semaphore to limit concurrent database connections
            int maxConcurrency = Math.Max(1, Environment.ProcessorCount / 2);
            using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

            var batchTasks = new List<Task<List<Domain.PDCalculation.LoanDetails>>>();

            for (int i = 0; i < totalBatches; i++)
            {
                int batchIndex = i;
                int skip = batchIndex * batchSize;
                int take = Math.Min(batchSize, totalCount - skip);

                Task<List<Domain.PDCalculation.LoanDetails>> batchTask = FetchBatchAsync(
                    skip, take, batchIndex + 1, totalBatches, semaphore, logger, cancellationToken);

                batchTasks.Add(batchTask);
            }

            var batchStopwatch = System.Diagnostics.Stopwatch.StartNew();
            List<Domain.PDCalculation.LoanDetails>[] batchResults = await Task.WhenAll(batchTasks);
            batchStopwatch.Stop();

            // Combine all batch results
            var combinedResults = new List<Domain.PDCalculation.LoanDetails>();
            foreach (List<Domain.PDCalculation.LoanDetails> batch in batchResults)
            {
                combinedResults.AddRange(batch);
            }

            logger.LogDebug("All {TotalBatches} batches completed in parallel in {ElapsedMs}ms, combined {TotalRecords} records",
                totalBatches, batchStopwatch.ElapsedMilliseconds, combinedResults.Count);

            return combinedResults;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during parallel loan details fetching");
            return new List<Domain.PDCalculation.LoanDetails>();
        }
    }

    /// <summary>
    /// Calculates optimal batch size for fetching data based on record count
    /// </summary>
    /// <param name="totalRecords">Total number of records to fetch</param>
    /// <returns>Optimal batch size</returns>
    private static int CalculateOptimalBatchSizeForFetch(int totalRecords)
    {
        return totalRecords switch
        {
            <= 50000 => 10000,    // Medium dataset - 10K per batch
            <= 200000 => 20000,   // Large dataset - 20K per batch  
            <= 500000 => 30000,   // Very large - 30K per batch
            _ => 40000             // Massive dataset - 40K per batch
        };
    }

    /// <summary>
    /// Fetches a single batch of loan details using a dedicated database context
    /// </summary>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <param name="batchNumber">Current batch number for logging</param>
    /// <param name="totalBatches">Total number of batches for logging</param>
    /// <param name="semaphore">Semaphore to limit concurrent operations</param>
    /// <param name="logger">Logger for debugging</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Batch of loan details</returns>
    private async Task<List<Domain.PDCalculation.LoanDetails>> FetchBatchAsync(
        int skip,
        int take,
        int batchNumber,
        int totalBatches,
        SemaphoreSlim semaphore,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            // Create a new scope and database context for this batch
            using IServiceScope scope = _serviceProvider.CreateScope();
            IApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var batchStopwatch = System.Diagnostics.Stopwatch.StartNew();

            List<Domain.PDCalculation.LoanDetails> batchResult = await dbContext.LoanDetails
                .AsNoTracking()
                .OrderBy(ld => ld.Id) // Ensure consistent ordering for pagination
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);

            batchStopwatch.Stop();
            logger.LogDebug("Batch {BatchNumber}/{TotalBatches} completed: {RecordCount} records in {ElapsedMs}ms",
                batchNumber, totalBatches, batchResult.Count, batchStopwatch.ElapsedMilliseconds);

            return batchResult;
        }
        finally
        {
            semaphore.Release();
        }
    }
}
