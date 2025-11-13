using Application.Abstractions.Calculations;
using Application.DTOs.PD;
using Application.Models.PDSummary;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.PdCalculation.ExecuteStep4Extrapolation;

/// <summary>
/// Executes Step 4 of PD calculation: Generates extrapolated PDs using interpolation data.
/// This step takes the average PD tables from Step 3 and performs extrapolation calculations
/// to generate probability of default values for all product categories and segments.
/// </summary>
internal sealed class Endpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("pd-calculations/test/step4-extrapolation", async (
            AveragePDTablesResponse request,
            IPDCalculationService pdCalculationService,
            ILogger<Endpoint> logger,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            logger.LogInformation("PD Calculation Step 4 - Extrapolation triggered");

            try
            {
                // Get user from context
                string createdBy = context.User?.Identity?.Name ?? "system";

                // Validate input data
                if (request?.AveragePDTables is null || request.AveragePDTables.Count == 0)
                {
                    logger.LogWarning("No average PD tables provided in request");
                    return Results.BadRequest("Average PD tables data is required for extrapolation.");
                }

                logger.LogInformation(
                    "Processing extrapolation for {ProductCategoryCount} product categories",
                    request.AveragePDTables.Count);

                // Execute Step 4 extrapolation logic
                Result<PdExtrapolationResultDto> result = await pdCalculationService.ExecuteStep4Async(
                    request,
                    createdBy,
                    cancellationToken);

                if (result.IsFailure)
                {
                    logger.LogWarning("Step 4 execution failed: {Error}", result.Error.Description);
                    return CustomResults.Problem(result);
                }

                logger.LogInformation("Step 4 - Extrapolation completed successfully");

                // Return the DTO as JSON
                return Results.Ok(result.Value);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during Step 4 - Extrapolation");
                return Results.Problem(new ProblemDetails
                {
                    Title = "PD Extrapolation Failed",
                    Detail = "An error occurred while performing extrapolation in Step 4. Please check the logs for more details.",
                    Status = 500,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
                });
            }
        })
        .WithTags("PD Calculation")
        .WithName("ExecutePdCalculationStep4Extrapolation")
        .WithSummary("Execute PD Calculation Step 4 - Extrapolation")
        .WithDescription("Performs extrapolation calculations to generate probability of default values using interpolation data from Step 3.")
        .Produces<PdExtrapolationResultDto>(200)
        .ProducesProblem(400)
        .ProducesProblem(500);
    }
}
