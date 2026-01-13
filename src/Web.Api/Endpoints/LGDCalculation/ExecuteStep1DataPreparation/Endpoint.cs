using Application.Abstractions.Calculations;
using Application.Models;
using SharedKernel;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LgdCalculation.ExecuteStep1DataPreparation;

/// <summary>
/// Executes Step 1 of LGD calculation: Placeholder implementation for LGD data preparation.
/// </summary>
internal sealed class Endpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("lgd-calculations/step1-data-preparation", async (
            ILgdCalculationService lgdCalculationService,
            ILogger<Endpoint> logger,
            HttpContext context,
            Request request,
            CancellationToken cancellationToken) =>
        {
            logger.LogInformation("{CalculationType} Calculation Step 1 - Data Preparation triggered", request.Type);

            try
            {
                // Get user from context
                string createdBy = context.User?.Identity?.Name ?? "system";

                LgdCalculationType calculationType = request.GetCalculationType();
                Result result = await lgdCalculationService.ExecuteStep1Async(createdBy, calculationType, cancellationToken);

                if (result.IsFailure)
                {
                    logger.LogWarning("{CalculationType} Step 1 execution failed: {Error}", calculationType, result.Error.Description);
                    return CustomResults.Problem(result);
                }

                logger.LogInformation("{CalculationType} Step 1 - Data Preparation completed successfully", calculationType);

                Response response = new(
                    Success: true,
                    Message: $"{calculationType} Calculation Step 1 - Data Preparation completed successfully",
                    Timestamp: DateTime.UtcNow
                );

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during {CalculationType} Step 1 - Data Preparation", request.Type);
                return Results.Problem(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Title = $"{request.Type} Data Preparation Failed",
                    Detail = ex.Message,
                    Status = 500,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
                });
            }
        })
        .WithTags("LGD Calculation")
        .WithName("ExecuteLgdStep1DataPreparation")
        .WithSummary("Execute LGD Calculation Step 1")
        .WithDescription("Executes Step 1 of LGD calculation - Data Preparation. Supports both standard LGD and VC_LGD calculations based on request parameter.");
    }
}

/// <summary>
/// Request model for LGD calculation operations
/// </summary>
/// <param name="Type">Type of calculation to perform (LGD or VC_LGD)</param>
public sealed record Request(string Type = "LGD")
{
    /// <summary>
    /// Gets the parsed LGD calculation type from the string value
    /// </summary>
    /// <returns>The corresponding LgdCalculationType enum value</returns>
    public LgdCalculationType GetCalculationType()
    {
        return Type?.ToUpperInvariant() switch
        {
            "LGD" => LgdCalculationType.LGD,
            "VC_LGD" => LgdCalculationType.VC_LGD,
            "VCLGD" => LgdCalculationType.VC_LGD,
            "VC-LGD" => LgdCalculationType.VC_LGD,
            _ => LgdCalculationType.LGD // Default to LGD for any unrecognized input
        };
    }
};

/// <summary>
/// Response model for LGD calculation operations
/// </summary>
/// <param name="Success">Indicates if the operation was successful</param>
/// <param name="Message">Description of the operation result</param>
/// <param name="Timestamp">When the operation completed</param>
public sealed record Response(bool Success, string Message, DateTime Timestamp);