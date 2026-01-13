using System.Text.Json;
using Application.Abstractions.Calculations;
using Application.DTOs.LGDCalculation;
using SharedKernel;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LgdCalculation.ExecuteStep3YearlyLgdAverage;

/// <summary>
/// Executes Step 3 of LGD calculation: Yearly LGD average calculation that computes LGD averages and changes by financial year.
/// </summary>
internal sealed class Endpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("lgd-calculations/step3-yearly-lgd-average", async (
            HttpRequest request,
            ILgdCalculationService lgdCalculationService,
            ILogger<Endpoint> logger,
            CancellationToken cancellationToken) =>
        {
            logger.LogInformation("LGD Calculation Step 3 - Yearly LGD Average Calculation triggered");

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
                    return Results.BadRequest("Request body cannot be empty. Please provide a valid JSON payload with Step 2 hierarchical calculation result.");
                }

                logger.LogDebug("Received request body with length: {Length}", requestBody.Length);

                // Parse the JSON payload
                JsonSerializerOptions jsonOptions = new()
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                Step3YearlyLgdAverageRequest? requestPayload;
                try
                {
                    requestPayload = JsonSerializer.Deserialize<Step3YearlyLgdAverageRequest>(requestBody, jsonOptions);
                }
                catch (JsonException jsonEx)
                {
                    logger.LogWarning(jsonEx, "Failed to deserialize request payload");
                    return Results.BadRequest($"Invalid JSON format: {jsonEx.Message}");
                }

                if (requestPayload?.Data is null)
                {
                    logger.LogWarning("Request payload data is null");
                    return Results.BadRequest("Invalid request payload. 'data' field is required and must contain Step 2 hierarchical calculation result.");
                }

                logger.LogInformation("Successfully parsed request payload with {FacilityCount} facilities and {YearCount} years",
                    requestPayload.Data.TotalFacilities, requestPayload.Data.YearClassifications.Count);

                Result<Step3YearlyLgdAverageResult> result = await lgdCalculationService.ExecuteStep3Async(requestPayload.Data, cancellationToken);

                if (result.IsFailure)
                {
                    logger.LogWarning("LGD Step 3 Yearly LGD Average execution failed: {Error}", result.Error.Description);
                    return CustomResults.Problem(result);
                }

                logger.LogInformation("LGD Step 3 - Yearly LGD Average Calculation completed successfully");

                // Return only the min/max years as requested
                return Results.Ok(result.Value);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error occurred during LGD Step 3 - Yearly LGD Average Calculation");
                return Results.Problem(
                    detail: ex.Message,
                    title: "LGD Step 3 Yearly LGD Average Calculation Error",
                    statusCode: 500);
            }
        })
        .DisableAntiforgery()
        .WithTags("LGD Calculation")
        .WithName("ExecuteLgdCalculationStep3YearlyLgdAverage")
        .WithSummary("Execute LGD Calculation Step 3 - Yearly LGD Average")
        .WithDescription("Executes Step 3 of LGD calculation - Yearly LGD average calculation that computes LGD averages and changes by financial year from Step 2 hierarchical results. Accepts raw JSON payload containing Step 2 data.")
        .Accepts<string>("application/json")
        .Produces<Step3YearlyLgdAverageResult>(200, contentType: "application/json")
        .ProducesProblem(400)
        .ProducesProblem(500);
    }
}