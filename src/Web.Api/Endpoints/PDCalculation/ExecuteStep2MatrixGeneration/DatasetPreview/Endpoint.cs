using Application.Abstractions.Calculations;
using SharedKernel;

namespace Web.Api.Endpoints.PdCalculation.ExecuteStep2MatrixGeneration.DatasetPreview;

internal sealed class Endpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("pd-calculations/test/step2-matrix-generation/dataset-preview", async (
            Request request,
            IPDCalculationService pdCalculationService,
            ILogger<Endpoint> logger,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyList<Application.DTOs.PD.PdMigrationDataset>> result = await pdCalculationService.GetStep2DatasetAsync(
                request.TimeConfig,
                request.DatePassedDueBuckets,
                request.PdConfig.PdConfiguration,
                cancellationToken);

            if (result.IsFailure)
            {
                logger.LogError("Failed to retrieve Step 2 dataset preview: {Error}", result.Error.Description);
                return Results.BadRequest(new
                {
                    success = false,
                    error = result.Error.Code,
                    message = result.Error.Description,
                    timestamp = DateTime.UtcNow
                });
            }

            Response response = new()
            {
                Datasets = result.Value,
                TotalCount = result.Value.Count,
                Timestamp = DateTime.UtcNow
            };

            logger.LogInformation("Successfully retrieved {Count} datasets for Step 2 preview", response.TotalCount);
            return Results.Ok(response);
        })
        .WithName("GetStep2DatasetPreview")
        .WithTags("PD Calculation")
        .Produces<Response>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);
    }
}
