using System.Text.Json;
using Application.Abstractions.Calculations;
using Application.DTOs.LGDCalculation;
using SharedKernel;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LgdCalculation.ExecuteStep4VcPointDetermination;

/// <summary>
/// Executes Step 4 of LGD calculation: VC-point determination that identifies optimal conversion point.
/// </summary>
internal sealed class Endpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("lgd-calculations/step4-vc-point-determination", async (
            HttpRequest request,
            ILgdCalculationService lgdCalculationService,
            ILogger<Endpoint> logger,
            CancellationToken cancellationToken) =>
        {
            logger.LogInformation("LGD Calculation Step 4 - VC-Point Determination triggered");

            try
            {
                // Read the request body
                string requestBody;
                using (var reader = new StreamReader(request.Body))
                {
                    requestBody = await reader.ReadToEndAsync(cancellationToken);
                }

                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    logger.LogWarning("Request body is empty or null");
                    return Results.BadRequest("Request body cannot be empty. Please provide a valid JSON payload with Step 3 yearly LGD average result.");
                }

                logger.LogDebug("Received request body with length: {Length}", requestBody.Length);

                // Parse the JSON payload
                JsonSerializerOptions jsonOptions = new()
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                Step4VcPointDeterminationRequest? requestPayload;
                try
                {
                    requestPayload = JsonSerializer.Deserialize<Step4VcPointDeterminationRequest>(requestBody, jsonOptions);
                }
                catch (JsonException jsonEx)
                {
                    logger.LogWarning(jsonEx, "Failed to deserialize request payload");
                    return Results.BadRequest($"Invalid JSON format: {jsonEx.Message}");
                }

                if (requestPayload?.Data is null)
                {
                    logger.LogWarning("Request payload data is null");
                    return Results.BadRequest("Invalid request payload. 'data' field is required and must contain Step 3 yearly LGD average result.");
                }

                logger.LogInformation("Successfully parsed request payload with {ClassificationCount} classifications using {Method} method",
                    requestPayload.Data.OverallAverageLgdByClassificationAndYears.Count, requestPayload.Method);

                Result<Step4VcPointDeterminationResult> result = await lgdCalculationService.ExecuteStep4Async(
                    requestPayload.Data, requestPayload.Method, cancellationToken);

                if (result.IsFailure)
                {
                    logger.LogWarning("LGD Step 4 VC-point determination execution failed: {Error}", result.Error.Description);
                    return CustomResults.Problem(result);
                }

                logger.LogInformation("LGD Step 4 - VC-Point Determination completed successfully");

                return Results.Ok(result.Value);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error occurred during LGD Step 4 - VC-Point Determination");
                return Results.Problem(
                    detail: ex.Message,
                    title: "LGD Step 4 VC-Point Determination Error",
                    statusCode: 500);
            }
        })
        .DisableAntiforgery()
        .WithTags("LGD Calculation")
        .WithName("ExecuteLgdCalculationStep4VcPointDetermination")
        .WithSummary("Execute LGD Calculation Step 4 - VC-Point Determination")
        .WithDescription("Executes Step 4 of LGD calculation - VC-point determination that identifies optimal conversion point from Step 3 yearly LGD average results. Accepts raw JSON payload containing Step 3 data.")
        .Accepts<string>("application/json")
        .Produces<Step4VcPointDeterminationResult>(200, contentType: "application/json")
        .ProducesProblem(400)
        .ProducesProblem(500);
    }
}