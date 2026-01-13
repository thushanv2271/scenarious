using System.Security.Claims;
using System.Text.Json;
using Application.Abstractions.Calculations;
using Application.Abstractions.Data;
using Application.DTOs.PD;
using Application.Models;
using Application.Models.PDSummary;
using Domain.PDAlgorithmResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.PdCalculation.ExecutePdCalculationPipeline;

/// <summary>
/// Executes the full PD calculation process (steps 1-4 sequentially):
/// Step 1: Reads Excel sheets, computes maturity dates, saves data to DB
/// Step 2: Builds combined dataset and generates migration matrices
/// Step 3: Generates historical PD and interpolation tables
/// Step 4: Generates extrapolated PDs using interpolation data
/// </summary>
internal sealed class Endpoint : IEndpoint
{
    private static readonly JsonSerializerOptions JsonDeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions JsonSerializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("pd-calculations/pipeline", async (
            Request request,
            IPDCalculationService pdCalculationService,
            IApplicationDbContext dbContext,
            ILogger<Endpoint> logger,
            CancellationToken cancellationToken,
            HttpContext context) =>
        {
            logger.LogInformation("PD Calculation pipeline execution triggered");

            try
            {
                // Extract UserId from JWT token claims ...
                string? userIdString = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid createdByUserId))
                {
                    var failure = Result.Failure<PdExtrapolationResultDto>(new Error(
                        "InvalidToken",
                        "Invalid token: UserId not found",
                        ErrorType.Validation
                    ));
                    return CustomResults.Problem(failure);
                }

                // Get user name from context (optional, for logging purposes)
                string createdBy = context.User?.Identity?.Name ?? "system";

                #region Step 1 - Data Preparation

                Result result = await pdCalculationService.ExecuteStep1Async(
                    request.QuarterEndedDates,
                    request.DatePassedDueBuckets,
                    request.FinalBucketPayload,
                    createdBy,
                    request.TimeConfig.Frequency);

                if (!result.IsSuccess)
                {
                    logger.LogWarning("Step 1 of PD Calculation Pipeline failed: {Error}", result.Error.Description);
                    throw new InvalidOperationException($"Step 1 failed: {result.Error.Description}");
                }
                logger.LogInformation("Step 1 of PD Calculation Pipeline executed successfully.");

                #endregion

                #region Step 2 - Generate Migration Matrices

                Result<IReadOnlyList<PeriodMigrationMatrix>> step2Result = await pdCalculationService.ExecuteStep2Async(
                    request.TimeConfig,
                    request.DatePassedDueBuckets,
                    request.PdConfig.PdConfiguration,
                    createdBy,
                    cancellationToken);

                if (!step2Result.IsSuccess)
                {
                    logger.LogWarning("Step 2 of PD Calculation Pipeline failed: {Error}", step2Result.Error.Description);
                    throw new InvalidOperationException($"Step 2 failed: {step2Result.Error.Description}");
                }

                IReadOnlyList<PeriodMigrationMatrix> migrationMatrices = step2Result.Value;
                logger.LogInformation("Step 2 of PD Calculation Pipeline executed successfully.");

                #endregion

                #region Step 3 - Historical PD Generation

                logger.LogInformation("Starting Step 3: PD Summary table generation from migration matrices");
                MigrationMatricesAndSummaryResponse mappedResponse = MapToMigrationMatricesAndSummaryResponse(migrationMatrices);
                logger.LogInformation("Mapped {MatricesCount} migration matrices to summary response format", mappedResponse.Matrices.Count);

                Result<string> step3Result = await pdCalculationService.ExecuteStep3Async(mappedResponse, cancellationToken);

                if (!step3Result.IsSuccess)
                {
                    logger.LogWarning("Step 3 of PD Calculation Pipeline failed: {Error}", step3Result.Error.Description);
                    throw new InvalidOperationException($"Step 3 failed: {step3Result.Error.Description}");
                }

                logger.LogInformation("Step 3 of PD Calculation Pipeline executed successfully - PD Summary tables generated");

                #endregion

                #region Step 4 - Extrapolation

                AveragePDTablesResponse? step3Data = JsonSerializer.Deserialize<AveragePDTablesResponse>(step3Result.Value, JsonDeserializeOptions);

                Result<PdExtrapolationResultDto> step4Result = await pdCalculationService.ExecuteStep4Async(
                    step3Data,
                    createdBy,
                    cancellationToken);

                if (!step4Result.IsSuccess)
                {
                    logger.LogWarning("Step 4 of PD Calculation Pipeline failed: {Error}", step4Result.Error.Description);
                    throw new InvalidOperationException($"Step 4 failed: {step4Result.Error.Description}");
                }

                logger.LogInformation("Step 4 of PD Calculation Pipeline executed successfully.");

                #endregion

                #region Save PD Algorithm Result to Database

                try
                {
                    // Check if there are existing records and delete them
                    bool hasExistingRecords = await dbContext.PDAlgorithmResults.AnyAsync(cancellationToken);

                    if (hasExistingRecords)
                    {
                        logger.LogInformation("Existing PD Algorithm Results found. Deleting all existing records...");

                        // Get all existing records
                        List<PDAlgorithmResult> existingResults = await dbContext.PDAlgorithmResults
                            .ToListAsync(cancellationToken);

                        int existingCount = existingResults.Count;

                        // Remove all existing records
                        dbContext.PDAlgorithmResults.RemoveRange(existingResults);
                        await dbContext.SaveChangesAsync(cancellationToken);

                        logger.LogInformation("Deleted {Count} existing PD Algorithm Result records", existingCount);
                    }
                    else
                    {
                        logger.LogInformation("No existing PD Algorithm Results found");
                    }

                    // Serialize step4 result to JSON for JSONB storage
                    string pdAlgorithmJson = JsonSerializer.Serialize(step4Result.Value, JsonSerializeOptions);

                    // Create the PD Algorithm Result entity with the extracted userId
                    var pdAlgorithmResult = PDAlgorithmResult.Create(
                        pdAlgorithmJson,
                        createdByUserId
                    );

                    // Save to database
                    dbContext.PDAlgorithmResults.Add(pdAlgorithmResult);
                    await dbContext.SaveChangesAsync(cancellationToken);

                    logger.LogInformation(
                        "PD Algorithm Result saved to database. ID: {Id}, ProductCategories: {Count}, CreatedBy: {UserId}",
                        pdAlgorithmResult.Id,
                        step4Result.Value.ProductCategories.Count,
                        createdByUserId);
                }
                catch (Exception saveEx)
                {
                    logger.LogError(saveEx, "Failed to save PD Algorithm Result to database: {ErrorMessage}", saveEx.Message);
                    // Continue execution - still return the calculated result
                }

                #endregion

                logger.LogInformation("✅ PD Calculation Pipeline completed successfully. All 4 steps executed without error.");

                return Results.Ok(step4Result.Value);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while executing PD Calculation pipeline");
                return Results.Problem(new ProblemDetails
                {
                    Title = "PD Calculation Pipeline Execution Failed",
                    Detail = $"An error occurred while executing the PD Calculation pipeline: {ex.Message}",
                    Status = 500,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
                });
            }
        })
        .WithTags("PD Calculation")
        .WithName("ExecutePdCalculationPipeline")
        .WithSummary("Execute PD Calculation Pipeline")
        .WithDescription("Executes the full PD Calculation pipeline, running all four steps: data extraction, merging, interpolation, and extrapolation. Results are saved to pd_algorithm_results table.")
        .Produces<PdExtrapolationResultDto>(200)
        .ProducesProblem(400)
        .ProducesProblem(500);
    }

    private static MigrationMatricesAndSummaryResponse MapToMigrationMatricesAndSummaryResponse(
        IReadOnlyList<PeriodMigrationMatrix> periodMigrationMatrices)
    {
        MigrationMatricesAndSummaryResponse response = new()
        {
            Success = true,
            Message = "Migration matrices generated successfully",
            Timestamp = DateTime.UtcNow,
            Matrices = periodMigrationMatrices.Select(pm => new Application.Models.PDSummary.MigrationMatrix
            {
                PeriodNMinus1 = pm.PeriodNMinus1,
                PeriodN = pm.PeriodN,
                ProductCategories = pm.ProductCategories.Select(pc => new ProductCategory
                {
                    ProductCategoryName = pc.ProductCategoryName,
                    Segments = pc.Segments.Select(seg => new Application.Models.PDSummary.Segment
                    {
                        SegmentName = seg.SegmentName,
                        ComparisonPeriod = seg.ComparisonPeriod,
                        Buckets = seg.Buckets,
                        BucketCount = seg.BucketCount,
                        HasPercentageData = seg.HasPercentageData,
                        Counts = new MatrixCounts
                        {
                            Matrix = seg.Counts.Matrix.Select(row => row.ToList()).ToList(),
                            ExitCounts = seg.Counts.ExitCounts.ToList(),
                            RowTotals = seg.Counts.RowTotals.ToList()
                        },
                        Percentages = seg.Percentages is not null ? new MatrixPercentages
                        {
                            Matrix = seg.Percentages.Matrix.Select(row => row.ToList()).ToList(),
                            ExitPercentages = seg.Percentages.ExitPercentages.ToList(),
                            GrandTotal = seg.Percentages.GrandTotal.Select(d => (double?)d).ToList()
                        } : null
                    }).ToList()
                }).ToList()
            }).ToList()
        };

        return response;
    }
}


