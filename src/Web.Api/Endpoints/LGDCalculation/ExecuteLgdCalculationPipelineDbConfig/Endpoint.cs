using Application.Abstractions.Pipeline;
using Application.DTOs.LGDCalculation;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LgdCalculation.ExecuteLgdCalculationPipelineDbConfig;

/// <summary>
/// Executes the LGD calculation pipeline using configuration stored in the database
/// </summary>
internal sealed class Endpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("lgd-calculations/pipeline-db-config", async (
            Request request,
            ILgdPipelineService lgdPipelineService,
            ILogger<Endpoint> logger,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            logger.LogInformation("LGD Calculation pipeline execution with DB config triggered for SessionId: {SessionId}", request.SessionId);

            try
            {
                // Get user from context
                string createdBy = context.User?.Identity?.Name ?? "system";

                Result<Step5FinancialYearLgdResult> result = await lgdPipelineService.RunPipelineFromDbAsync(createdBy, request.SessionId, cancellationToken);

                if (result.IsFailure)
                {
                    logger.LogError("LGD Calculation Pipeline with DB config failed for SessionId: {SessionId}. Error: {Error}",
                        request.SessionId, result.Error);

                    return Results.BadRequest(new ProblemDetails
                    {
                        Title = "LGD Calculation Pipeline Execution Failed",
                        Detail = result.Error.Description,
                        Status = 400,
                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                    });
                }

                logger.LogInformation("LGD Calculation Pipeline with DB config completed successfully for SessionId: {SessionId}", request.SessionId);

                return Results.Ok(new Response
                {
                    Result = result.Value,
                    Success = true,
                    Message = "LGD Calculation pipeline executed successfully",
                    SessionId = request.SessionId
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while executing LGD Calculation pipeline with DB config for SessionId: {SessionId}", request.SessionId);
                return Results.Problem(new ProblemDetails
                {
                    Title = "LGD Calculation Pipeline Execution Failed",
                    Detail = $"An error occurred while executing the LGD Calculation pipeline: {ex.Message}",
                    Status = 500,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
                });
            }
        })
        .WithTags("LGD Calculation")
        .WithName("ExecuteLgdCalculationPipelineDbConfig")
        .WithSummary("Execute LGD Calculation Pipeline using Database Configuration")
        .WithDescription("Executes the full LGD Calculation pipeline using configuration retrieved from the database based on the provided SessionId. This includes steps 1-6: LGD data preparation, discounted cashflow summary, yearly LGD average calculation (optional), VC-point determination (optional), VC_LGD processing, financial year analysis, and final result combination.")
        .Produces<Response>(200)
        .ProducesProblem(400)
        .ProducesProblem(500);
    }
}