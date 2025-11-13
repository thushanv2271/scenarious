using Application.Abstractions.Calculations;
using SharedKernel;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.PdCalculation.ExecuteStep1DataPreparation;

/// <summary>
/// Executes Step 1 of PD calculation: Reads Excel sheets, computes maturity dates, and saves data to the database.
/// This step prepares all the necessary data for subsequent PD calculation steps.
/// </summary>
internal sealed class Endpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("pd-calculations/test/step1-data-preparation", async (
            Request request,
            IPDCalculationService pdCalculationService,
            ILogger<Endpoint> logger,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            logger.LogInformation("PD Calculation Step 1 - Data Preparation triggered");

            try
            {
                // Get user from context
                string createdBy = context.User?.Identity?.Name ?? "system";

                Result result = await pdCalculationService.ExecuteStep1Async(
                        request.QuarterEndedDates,
                        request.DatePassedDueBuckets,
                        request.FinalBucketPayload,
                        createdBy,
                        request.TimeConfig.Frequency,
                        cancellationToken);

                if (result.IsFailure)
                {
                    logger.LogWarning("Step 1 execution failed: {Error}", result.Error.Description);
                    return CustomResults.Problem(result);
                }

                logger.LogInformation("Step 1 - Data Preparation completed successfully");

                Response response = new(
                            Success: true,
                            Message: "PD Calculation Step 1 - Data Preparation completed successfully",
                            Timestamp: DateTime.UtcNow
                );

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during Step 1 - Data Preparation");
                return Results.Problem(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Title = "Data Preparation Failed",
                    Detail = "An error occurred while preparing data in Step 1. Please check the logs for more details.",
                    Status = 500,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
                });
            }
        })
        .WithTags("PD Calculation")
        .WithName("ExecutePdCalculationStep1DataPreparation")
        .WithSummary("Execute PD Calculation Step 1 - Data Preparation")
        .WithDescription("Executes Step 1 of PD calculation: file extraction, maturity date computation, and database insertion.")
        .Produces<Response>(200)
        .ProducesProblem(400)
        .ProducesProblem(500);
    }
}
