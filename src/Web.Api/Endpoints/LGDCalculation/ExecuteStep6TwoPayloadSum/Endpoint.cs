using Application.Abstractions.Calculations;
using Application.DTOs.LGDCalculation;
using SharedKernel;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LgdCalculation.ExecuteStep6TwoPayloadSum;

/// <summary>
/// Executes Step 6 of LGD calculation: Sum of two Step 5 financial year LGD analysis results.
/// </summary>
internal sealed class Endpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("lgd-calculations/step6-two-payload-sum", async (
            ExecuteStep6TwoPayloadSumRequest request,
            ILgdCalculationService lgdCalculationService,
            ILogger<Endpoint> logger,
            CancellationToken cancellationToken) =>
        {
            try
            {
                // Validate request data
                if (request.Payload1 is null)
                {
                    logger.LogWarning("Payload1 is null in Step 6 request");
                    return Results.BadRequest("Payload1 is required");
                }

                if (request.Payload2 is null)
                {
                    logger.LogWarning("Payload2 is null in Step 6 request");
                    return Results.BadRequest("Payload2 is required");
                }

                logger.LogInformation("Starting Step 6 two payload sum analysis via Web API");

                // Execute Step 6 calculation
                Result<Step5FinancialYearLgdResult> result = await lgdCalculationService.ExecuteStep6Async(
                    request.Payload1,
                    request.Payload2,
                    cancellationToken);

                if (result.IsFailure)
                {
                    logger.LogWarning("Step 6 two payload sum analysis failed: {Error}", result.Error.Description);
                    return Results.BadRequest(new
                    {
                        error = result.Error.Code,
                        message = result.Error.Description
                    });
                }

                logger.LogInformation("Step 6 two payload sum analysis completed successfully");

                return Results.Ok(result.Value);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error occurred during Step 6 two payload sum analysis");
                return Results.Problem(
                    detail: "An internal server error occurred during calculation",
                    statusCode: 500,
                    title: "Internal Server Error");
            }
        })
        .WithName("ExecuteStep6TwoPayloadSum")
        .WithTags("LGD Calculation")
        .WithSummary("Execute Step 6: Sum Two Step 5 Payloads")
        .WithDescription("Calculates the sum of two Step 5 financial year LGD analysis results by financial year end date and classification")
        .Accepts<ExecuteStep6TwoPayloadSumRequest>("application/json")
        .Produces<Step5FinancialYearLgdResult>(200)
        .ProducesProblem(400)
        .ProducesProblem(500);
    }
}