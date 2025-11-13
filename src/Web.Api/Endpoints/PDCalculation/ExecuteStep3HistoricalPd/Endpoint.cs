using System.Text.Json;
using Application.Abstractions.Calculations;
using Application.Models.PDSummary;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.PdCalculation.ExecuteStep3HistoricalPd;

/// <summary>
/// Executes Step 3 of PD calculation: Generates historical PD and interpolation tables.
/// This step processes migration matrices to create average PD tables with both historical
/// and interpolated probability of default values for each product category and segment.
/// </summary>
internal sealed class Endpoint : IEndpoint
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("pd-calculations/test/step3-historical-pd", async (
            IFormFile file,
            [FromServices] IPDCalculationService pdCalculationService,
            [FromServices] ILogger<Endpoint> logger,
            CancellationToken cancellationToken) =>
        {
            logger.LogInformation("PD Calculation Step 3 - Historical PD Generation triggered");

            try
            {
                // Validate the uploaded file
                if (file is null || file.Length == 0)
                {
                    logger.LogWarning("No file provided or file is empty");
                    return Results.BadRequest("Please provide a valid JSON file.");
                }

                if (!file.ContentType.Equals("application/json", StringComparison.OrdinalIgnoreCase) &&
                 !file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning("Invalid file type provided: {ContentType}, {FileName}", file.ContentType, file.FileName);
                    return Results.BadRequest("Only JSON files are allowed.");
                }

                // Read the JSON content from the uploaded file
                using Stream stream = file.OpenReadStream();
                using StreamReader reader = new(stream);
                string jsonContent = await reader.ReadToEndAsync(cancellationToken);

                logger.LogInformation("Successfully read JSON file content, size: {Size} bytes", jsonContent.Length);

                MigrationMatricesAndSummaryResponse? migrationData =
                 JsonSerializer.Deserialize<MigrationMatricesAndSummaryResponse>(jsonContent, JsonOptions);

                if (migrationData is null)
                {
                    logger.LogWarning("Failed to deserialize migration data from uploaded file");
                    return Results.BadRequest("Failed to deserialize migration data from the uploaded file.");
                }

                logger.LogInformation("Successfully deserialized migration data with {Count} matrices", migrationData.Matrices.Count);

                Result<string> result = await pdCalculationService.ExecuteStep3Async(migrationData, cancellationToken);

                if (result.IsFailure)
                {
                    logger.LogWarning("Step 3 execution failed: {Error}", result.Error.Description);
                    return CustomResults.Problem(result);
                }

                logger.LogInformation("Step 3 - Historical PD Generation completed successfully");

                // Parse the JSON result to return structured data
                var jsonDoc = JsonDocument.Parse(result.Value);

                return Results.Ok(jsonDoc.RootElement);
            }
            catch (JsonException jsonEx)
            {
                logger.LogError(jsonEx, "JSON parsing error occurred during Step 3 - Historical PD Generation");
                return Results.Problem(new ProblemDetails
                {
                    Title = "JSON Parsing Failed",
                    Detail = $"Failed to parse JSON data: {jsonEx.Message}",
                    Status = 400,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during Step 3 - Historical PD Generation");
                return Results.Problem(new ProblemDetails
                {
                    Title = "Historical PD Generation Failed",
                    Detail = "An error occurred while generating historical PD tables in Step 3. Please check the logs for more details.",
                    Status = 500,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
                });
            }
        })
        .DisableAntiforgery()
        .WithTags("PD Calculation")
        .WithName("ExecutePdCalculationStep3HistoricalPd")
        .WithSummary("Execute PD Calculation Step 3 - Generate Historical PD")
        .WithDescription("Generates historical PD and interpolation tables from uploaded migration matrices JSON file.")
        .Produces(200, contentType: "application/json")
        .ProducesProblem(400)
        .ProducesProblem(500);
    }
}
