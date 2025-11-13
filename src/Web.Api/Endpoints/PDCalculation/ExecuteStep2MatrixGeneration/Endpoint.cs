using System.Linq;
using Application.Abstractions.Calculations;
using Application.DTOs.PD;
using Application.Models;
using SharedKernel;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.PdCalculation.ExecuteStep2MatrixGeneration;

/// <summary>
/// Executes Step 2 of PD calculation: Builds combined dataset and generates migration matrices.
/// This step creates migration matrices for PD analysis from available datasets, including both
/// count data and percentage calculations with Grand Total column.
/// </summary>
internal sealed class Endpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("pd-calculations/test/step2-matrix-generation", async (
            Request request,
            IPDCalculationService pdCalculationService,
            ILogger<Endpoint> logger,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            logger.LogInformation("PD migration matrices generation triggered (Step 2)");

            try
            {
                // Get user from context
                string createdBy = context.User?.Identity?.Name ?? "system";

                // Execute Step 2 using the orchestrated method
                Result<IReadOnlyList<PeriodMigrationMatrix>> result =
                        await pdCalculationService.ExecuteStep2Async(
                            request.TimeConfig,
                            request.DatePassedDueBuckets,
                            request.PdConfig.PdConfiguration,
                            createdBy,
                            cancellationToken);

                if (result.IsFailure)
                {
                    logger.LogWarning("Step 2 execution failed: {Error}", result.Error.Description);
                    return CustomResults.Problem(result);
                }

                IReadOnlyList<PeriodMigrationMatrix> matrices = result.Value;

                logger.LogInformation(
                    "Migration matrices generation completed successfully. Generated {Count} matrices with both counts and percentages",
                    matrices.Count);

                // Map to response model
                Response response = new(
                    Success: true,
                    Message: "PD Calculation Step 2 - Migration matrices generated successfully",
                    Timestamp: DateTime.UtcNow,
                    Matrices: matrices.Select(MapToMatrixResponse).ToList()
                 );

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while generating migration matrices (Step 2)");
                return Results.Problem(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Title = "Migration Matrix Generation Failed",
                    Detail = "An error occurred while generating the migration matrices in Step 2. Please check the logs for more details.",
                    Status = 500,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
                });
            }
        })
        .WithTags("PD Calculation")
        .WithName("ExecutePdCalculationStep2MatrixGeneration")
        .WithSummary("Execute PD Calculation Step 2 - Generate Migration Matrices")
        .WithDescription("Executes Step 2 of PD calculation: generates migration matrices for PD analysis from available datasets. Always includes both count data and percentage calculations with Grand Total column.")
        .Produces<Response>(200)
        .ProducesProblem(400)
        .ProducesProblem(500);
    }

    /// <summary>
    /// Maps a PeriodMigrationMatrix to the response model
    /// </summary>
    private static MatrixResponse MapToMatrixResponse(PeriodMigrationMatrix periodMatrix)
    {
        return new MatrixResponse(
            PeriodNMinus1: periodMatrix.PeriodNMinus1,
            PeriodN: periodMatrix.PeriodN,
            ProductCategories: periodMatrix.ProductCategories.Select(pc => new ProductCategoryResponse(
                ProductCategoryName: pc.ProductCategoryName,
                Segments: pc.Segments.Select(seg => new SegmentResponse(
                    SegmentName: seg.SegmentName,
                    ProductCategory: seg.ProductCategory,
                    ComparisonPeriod: seg.ComparisonPeriod,
                    Buckets: seg.Buckets,
                    BucketCount: seg.BucketCount,
                    HasPercentageData: seg.HasPercentageData,
                    Counts: new CountsResponse(
                        Matrix: seg.Counts.Matrix.Select(row => row.ToList()).ToList(),
                        ExitCounts: seg.Counts.ExitCounts.ToList(),
                        RowTotals: seg.Counts.RowTotals.ToList()
                    ),
                    Percentages: seg.Percentages is not null ? new PercentagesResponse(
                        Matrix: seg.Percentages.Matrix.Select(row => row.ToList()).ToList(),
                        ExitPercentages: seg.Percentages.ExitPercentages.ToList(),
                        GrandTotal: seg.Percentages.GrandTotal.ToList()
                    )
                    : null
                )).ToList()
            )).ToList()
        );
    }
}
