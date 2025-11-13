using System.Text.Json;
using Application.Abstractions.Calculations;
using Application.DTOs.PD;
using Application.Models;
using Application.Models.PDSummary;
using Microsoft.AspNetCore.Mvc;
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
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("pd-calculations/pipeline", async (
            Request request,
            IPDCalculationService pdCalculationService,
            ILogger<Endpoint> logger,
            CancellationToken cancellationToken,
            HttpContext context) =>
        {
            logger.LogInformation("PD Calculation pipeline execution triggered");

            try
            {
                // Get user from context
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

                AveragePDTablesResponse? step3Data = JsonSerializer.Deserialize<AveragePDTablesResponse>(step3Result.Value, JsonOptions);

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

                logger.LogInformation("✅ PD Calculation Pipeline completed successfully. All 4 steps executed without error.");

                return Results.Ok(step4Result.Value);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while executing PD Calculation pipeline");
                return Results.Problem(new ProblemDetails
                {
                    Title = "PD Calculation Pipeline Execution Failed",
                    Detail = "An error occurred while executing the PD Calculation pipeline. Please check the logs for more details.",
                    Status = 500,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
                });
            }
        })
        .WithTags("PD Calculation")
        .WithName("ExecutePdCalculationPipeline")
        .WithSummary("Execute PD Calculation Pipeline")
        .WithDescription("Executes the full PD Calculation pipeline, running all four steps: data extraction, merging, interpolation, and extrapolation.")
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
