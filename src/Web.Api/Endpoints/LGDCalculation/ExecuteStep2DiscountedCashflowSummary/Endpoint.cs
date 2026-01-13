using Application.Abstractions.Calculations;
using Application.DTOs.LGDCalculation;
using Application.Models;
using SharedKernel;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LgdCalculation.ExecuteStep2DiscountedCashflowSummary;

/// <summary>
/// Executes Step 2 of LGD calculation: Hierarchical sum of discounted cashflows organized by Year > Segment > Facility.
/// </summary>
internal sealed class Endpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("lgd-calculations/step2-discounted-cashflow-summary", async (
            ILgdCalculationService lgdCalculationService,
            ILogger<Endpoint> logger,
            Request request,
            CancellationToken cancellationToken) =>
        {
            LgdCalculationType calculationType = LgdCalculationTypeHelper.Parse(request.CalculationType);
            logger.LogInformation("{CalculationType} Calculation Step 2 - Hierarchical Discounted Cashflow Summary triggered", calculationType);

            try
            {
                // Create VC Points dictionary based on request parameters
                Dictionary<string, decimal>? vcPointsByClassification = null;
                if (calculationType == LgdCalculationType.VC_LGD)
                {
                    if (request.VCPointsByClassification is not null && request.VCPointsByClassification.Count > 0)
                    {
                        // Use classification-specific VC points
                        vcPointsByClassification = new Dictionary<string, decimal>(request.VCPointsByClassification, StringComparer.OrdinalIgnoreCase);
                    }
                    else if (request.VCPoint.HasValue)
                    {
                        // Backward compatibility: use single VC point for all classifications
                        // We'll determine classifications dynamically in the service
                        vcPointsByClassification = null; // Will be handled in service with legacy VCPoint
                    }
                }

                Result<HierarchicalStep2LgdCalculationResult> result = await lgdCalculationService.ExecuteStep2Async(calculationType, request.VCPoint, vcPointsByClassification, cancellationToken);

                if (result.IsFailure)
                {
                    logger.LogWarning("{CalculationType} Step 2 hierarchical execution failed: {Error}", calculationType, result.Error.Description);
                    return CustomResults.Problem(result);
                }

                logger.LogInformation("{CalculationType} Step 2 - Hierarchical Summary completed successfully. {YearCount} years, {FacilityCount} facilities processed, Grand Total: {GrandTotal:C}",
                    calculationType, result.Value.YearClassifications.Count, result.Value.TotalFacilities, result.Value.GrandTotalDiscountedCashflows);

                // Log detailed summary by year and segment
                foreach (YearLgdClassification yearData in result.Value.YearClassifications.OrderBy(y => y.Year))
                {
                    logger.LogInformation("Year {Year}: {SegmentCount} segments, {FacilityCount} facilities, Total: {Total:C}",
                        yearData.Year, yearData.LgdClassifications.Count, yearData.TotalFacilities, yearData.TotalDiscountedCashflows);

                    foreach (SegmentLgdClassification segmentData in yearData.LgdClassifications.OrderBy(s => s.LgdClassification))
                    {
                        logger.LogInformation("  {Segment}: {FacilityCount} facilities, Total: {Total:C}, Avg LGD: {AvgLgd:P2}",
                            segmentData.LgdClassification, segmentData.TotalFacilities, segmentData.TotalDiscountedCashflows, segmentData.AverageLgd);
                    }
                }

                Response response = new(
                    Success: true,
                    Message: $"{calculationType} Calculation Step 2 hierarchical completed successfully. Processed {result.Value.YearClassifications.Count} years with {result.Value.TotalFacilities} unique facilities and grand total of {result.Value.GrandTotalDiscountedCashflows:C}",
                    Data: result.Value,
                    Timestamp: DateTime.UtcNow
                );

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during {CalculationType} Step 2 - Hierarchical Summary", calculationType);
                return Results.Problem(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Title = $"{calculationType} Step 2 Hierarchical Calculation Failed",
                    Detail = ex.Message,
                    Status = 500,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
                });
            }
        })
        .WithTags("LGD Calculation")
        .WithName("ExecuteLgdStep2DiscountedCashflowSummary")
        .WithSummary("Execute LGD Calculation Step 2 with Hierarchical Structure")
        .WithDescription("Executes Step 2 of LGD calculation with hierarchical organization: Year > LGD Classification (Segment) > Facility. Supports both standard LGD and VC_LGD calculations based on request parameter.")
        .Produces<Response>(200, "application/json")
        .ProducesProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(500);
    }
}

/// <summary>
/// Request model for LGD Step 2 calculation operations
/// </summary>
/// <param name="CalculationType">Type of calculation to perform ("LGD" or "VC_LGD")</param>
/// <param name="VCPoint">Legacy VC Point threshold value in years. Only used for VC_LGD calculations for backward compatibility.</param>
/// <param name="VCPointsByClassification">VC Point threshold values by classification (e.g., {"RETAIL": 1, "SME": 2, "CORPORATE": 1.5}). Only used for VC_LGD calculations. Takes precedence over VCPoint parameter.</param>
public sealed record Request(
    string CalculationType = "LGD",
    decimal? VCPoint = null,
    Dictionary<string, decimal>? VCPointsByClassification = null);

/// <summary>
/// Response model for LGD Step 2 hierarchical calculation operations
/// </summary>
/// <param name="Success">Indicates if the operation was successful</param>
/// <param name="Message">Description of the operation result</param>
/// <param name="Data">The hierarchical Step 2 calculation result organized by year, segment, and facility</param>
/// <param name="Timestamp">When the operation completed</param>
public sealed record Response(
    bool Success,
    string Message,
    HierarchicalStep2LgdCalculationResult? Data,
    DateTime Timestamp);