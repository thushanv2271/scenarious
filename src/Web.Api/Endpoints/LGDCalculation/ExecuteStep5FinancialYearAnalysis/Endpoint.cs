using System.Globalization;
using System.Text.Json;
using Application.Abstractions.Calculations;
using Application.DTOs.LGDCalculation;
using SharedKernel;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LgdCalculation.ExecuteStep5FinancialYearAnalysis;

/// <summary>
/// Executes Step 5 of LGD calculation: Financial year-based LGD analysis using Step 2 hierarchical data.
/// </summary>
internal sealed class Endpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("lgd-calculations/step5-financial-year-analysis", async (
            HttpRequest request,
            ILgdCalculationService lgdCalculationService,
            ILogger<Endpoint> logger,
            CancellationToken cancellationToken) =>
        {
            logger.LogInformation("LGD Calculation Step 5 - Financial Year Analysis triggered");

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
                    return Results.BadRequest("Request body cannot be empty. Please provide a valid JSON payload with Step 2 hierarchical result and financial year ends.");
                }

                logger.LogDebug("Received request body with length: {Length}", requestBody.Length);

                // Parse the JSON payload
                JsonSerializerOptions jsonOptions = new()
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                Step5FinancialYearLgdRequest? requestPayload;
                try
                {
                    requestPayload = JsonSerializer.Deserialize<Step5FinancialYearLgdRequest>(requestBody, jsonOptions);
                }
                catch (JsonException jsonEx)
                {
                    logger.LogWarning(jsonEx, "Failed to deserialize request payload");
                    return Results.BadRequest($"Invalid JSON format: {jsonEx.Message}");
                }

                if (requestPayload?.Data is null)
                {
                    logger.LogWarning("Request payload data is null");
                    return Results.BadRequest("Invalid request payload. 'data' field is required and must contain Step 2 hierarchical result.");
                }

                if (requestPayload.FinancialYearEnds is null || requestPayload.FinancialYearEnds.Count == 0)
                {
                    logger.LogWarning("Financial year ends list is null or empty");
                    return Results.BadRequest("Invalid request payload. 'financialYearEnds' field is required and must contain at least one financial year end date.");
                }

                // Parse financial year end dates from string format
                List<DateTime> financialYearEnds = new();
                List<string> parseErrors = new();

                foreach (string dateString in requestPayload.FinancialYearEnds)
                {
                    if (TryParseFinancialYearEndDate(dateString, out DateTime parsedDate))
                    {
                        financialYearEnds.Add(parsedDate);
                    }
                    else
                    {
                        parseErrors.Add($"'{dateString}' - expected format: DD-MMM-YY (e.g., 31-Dec-23)");
                    }
                }

                if (parseErrors.Count > 0)
                {
                    logger.LogWarning("Failed to parse {ErrorCount} financial year end dates", parseErrors.Count);
                    return Results.BadRequest($"Invalid financial year end date format(s): {string.Join("; ", parseErrors)}");
                }

                if (financialYearEnds.Count == 0)
                {
                    logger.LogWarning("No valid financial year end dates found");
                    return Results.BadRequest("No valid financial year end dates provided. Please provide dates in format DD-MMM-YY (e.g., 31-Dec-23).");
                }

                logger.LogInformation("Successfully parsed request payload with {YearCount} years, {ClassificationCount} classifications, {FinancialYearCount} financial year ends",
                    requestPayload.Data.YearClassifications.Count,
                    requestPayload.Data.YearClassifications.SelectMany(yc => yc.LgdClassifications).Select(c => c.LgdClassification).Distinct().Count(),
                    financialYearEnds.Count);

                // Log the financial year ends being processed
                logger.LogInformation("Processing financial year ends: {FinancialYearEnds}",
                    string.Join(", ", financialYearEnds.OrderBy(d => d).Select(d => d.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture))));

                Result<Step5FinancialYearLgdResult> result = await lgdCalculationService.ExecuteStep5Async(
                    requestPayload.Data, financialYearEnds, cancellationToken);

                if (result.IsFailure)
                {
                    logger.LogWarning("LGD Step 5 financial year analysis execution failed: {Error}", result.Error.Description);
                    return CustomResults.Problem(result);
                }

                logger.LogInformation("LGD Step 5 - Financial Year Analysis completed successfully");

                return Results.Ok(result.Value);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error occurred during LGD Step 5 - Financial Year Analysis");
                return Results.Problem(
                    detail: ex.Message,
                    title: "LGD Step 5 Financial Year Analysis Error",
                    statusCode: 500);
            }
        })
        .DisableAntiforgery()
        .WithTags("LGD Calculation")
        .WithName("ExecuteLgdCalculationStep5FinancialYearAnalysis")
        .WithSummary("Execute LGD Calculation Step 5 - Financial Year Analysis")
        .WithDescription("Executes Step 5 of LGD calculation - Financial year-based LGD analysis using Step 2 hierarchical data and financial year end dates. Accepts raw JSON payload containing Step 2 data and financial year ends in format DD-MMM-YY (e.g., 31-Dec-23).")
        .Accepts<string>("application/json")
        .Produces<Step5FinancialYearLgdResult>(200, contentType: "application/json")
        .ProducesProblem(400)
        .ProducesProblem(500);
    }

    /// <summary>
    /// Attempts to parse a financial year end date string in format DD-MMM-YY
    /// </summary>
    /// <param name="dateString">Date string to parse (e.g., "31-Dec-23")</param>
    /// <param name="parsedDate">Parsed date if successful</param>
    /// <returns>True if parsing was successful, false otherwise</returns>
    private static bool TryParseFinancialYearEndDate(string dateString, out DateTime parsedDate)
    {
        parsedDate = default;

        if (string.IsNullOrWhiteSpace(dateString))
        {
            return false;
        }

        // Try multiple formats to be flexible
        string[] formats = {
            "dd-MMM-yy",    // 31-Dec-23
            "dd-MMM-yyyy",  // 31-Dec-2023
            "dd/MMM/yy",    // 31/Dec/23
            "dd/MMM/yyyy",  // 31/Dec/2023
            "dd-MM-yy",     // 31-12-23
            "dd-MM-yyyy",   // 31-12-2023
            "dd/MM/yy",     // 31/12/23
            "dd/MM/yyyy"    // 31/12/2023
        };

        foreach (string format in formats)
        {
            if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
            {
                return true;
            }
        }

        // Also try standard parsing as fallback
        return DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate);
    }
}