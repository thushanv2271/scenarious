using Application.LGD.Services;
using Domain.LGDProgressTrackings;
using System.Globalization;
using Application.Abstractions.Calculations;
using Application.DTOs.LGDCalculation;
using Application.Models;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Infrastructure;
using Application.LGD.GetLGDProgress;
using Application.Abstractions.Messaging;

namespace Web.Api.Endpoints.LgdCalculation.ExecuteLgdCalculationPipeline;

/// <summary>
/// Executes the optimized LGD calculation pipeline (steps 1-6):
/// Step 1: Data Preparation - Executes for both LGD and VC_LGD types
/// Step 2: Discounted Cashflow Summary - Generates hierarchical summaries for LGD
/// Step 3: Yearly LGD Average - Computes LGD averages (only if VC points not provided)
/// Step 4: VC-Point Determination - Determines optimal conversion points (only if VC points not provided)
/// Step 2 VC_LGD: Discounted Cashflow Summary with VC points for VC_LGD
/// Step 5: Financial Year Analysis for both LGD and VC_LGD results
/// Step 6: Final Result Combination of both Step 5 results
/// </summary>
internal sealed class Endpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("lgd-calculations/pipeline", async (
            LgdCalculationPipelineRequest request,
            ILgdCalculationService lgdCalculationService,
            ILgdProgressPublisher progressPublisher,
            IQueryHandler<GetLgdProgressQuery, GetLgdProgressResponse> progressHandler,
            ILogger<Endpoint> logger,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            logger.LogInformation("LGD Calculation pipeline execution triggered");

            try
            {
                // Validate request
                if (request.FinancialYearEnds is null || request.FinancialYearEnds.Count == 0)
                {
                    logger.LogWarning("Financial year ends list is null or empty");
                    return Results.BadRequest("Invalid request payload. 'financialYearEnds' field is required and must contain at least one financial year end date.");
                }

                // Parse financial year end dates
                List<DateTime> financialYearEnds = new();
                List<string> parseErrors = new();

                foreach (string dateString in request.FinancialYearEnds)
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

                bool useProvidedVcPoints = request.VcPointsByClassification is not null && request.VcPointsByClassification.Count > 0;
                logger.LogInformation("Pipeline execution started with {UseProvidedVcPoints} VC points and {FinancialYearCount} financial year ends",
                    useProvidedVcPoints ? "provided" : "auto-determined", financialYearEnds.Count);

                Guid sessionId;

                // Use provided sessionId or create new progress records
                if (request.SessionId.HasValue)
                {
                    sessionId = request.SessionId.Value;
                    logger.LogInformation("Using provided LGD Pipeline Progress Session ID: {SessionId}", sessionId);
                }
                else
                {
                    // Initialize LGD progress tracking records
                    logger.LogInformation("Initializing new LGD progress tracking records...");
                    Result<GetLgdProgressResponse> progressResult = await progressHandler.Handle(
                        new GetLgdProgressQuery(IsRerun: false),
                        cancellationToken);

                    if (!progressResult.IsSuccess)
                    {
                        logger.LogError("Failed to initialize LGD progress tracking: {Error}", progressResult.Error.Description);
                        return Results.Problem("Failed to initialize progress tracking");
                    }

                    sessionId = progressResult.Value.SessionId;
                    logger.LogInformation("Created new LGD Pipeline Progress Session ID: {SessionId}", sessionId);
                }

                // Get user from context
                string createdBy = context.User?.Identity?.Name ?? "system";

                #region Step 1a - LGD Data Preparation

                logger.LogInformation("Starting Step 1a - LGD Data Preparation");
                await progressPublisher.PublishProgress(sessionId, 1, 1, LgdProgressStatus.InProgress, cancellationToken: cancellationToken);
                Result lgdStep1Result = await lgdCalculationService.ExecuteStep1Async(
                    createdBy,
                    LgdCalculationType.LGD,
                    cancellationToken);

                if (!lgdStep1Result.IsSuccess)
                {
                    logger.LogWarning("Step 1a (LGD) of LGD Calculation Pipeline failed: {Error}", lgdStep1Result.Error.Description);
                    await progressPublisher.PublishProgress(sessionId, 1, 1, LgdProgressStatus.Failed, lgdStep1Result.Error.Description, cancellationToken);
                    throw new InvalidOperationException($"Step 1a (LGD) failed: {lgdStep1Result.Error.Description}");
                }
                logger.LogInformation("Step 1a (LGD) of LGD Calculation Pipeline executed successfully.");
                await progressPublisher.PublishProgress(sessionId, 1, 1, LgdProgressStatus.Completed, cancellationToken: cancellationToken);

                #endregion

                #region Step 1b - VC_LGD Data Preparation

                logger.LogInformation("Starting Step 1b - VC_LGD Data Preparation");
                await progressPublisher.PublishProgress(sessionId, 1, 2, LgdProgressStatus.InProgress, cancellationToken: cancellationToken);
                Result vcLgdStep1Result = await lgdCalculationService.ExecuteStep1Async(
                    createdBy,
                    LgdCalculationType.VC_LGD,
                    cancellationToken);

                if (!vcLgdStep1Result.IsSuccess)
                {
                    logger.LogWarning("Step 1b (VC_LGD) of LGD Calculation Pipeline failed: {Error}", vcLgdStep1Result.Error.Description);
                    throw new InvalidOperationException($"Step 1b (VC_LGD) failed: {vcLgdStep1Result.Error.Description}");
                }
                logger.LogInformation("Step 1b (VC_LGD) of LGD Calculation Pipeline executed successfully.");
                await progressPublisher.PublishProgress(sessionId, 1, 2, LgdProgressStatus.Completed, cancellationToken: cancellationToken);

                #endregion

                #region Step 2 - Discounted Cashflow Summary

                logger.LogInformation("Starting Step 2 - LGD Discounted Cashflow Summary");
                await progressPublisher.PublishProgress(sessionId, 2, 1, LgdProgressStatus.InProgress, cancellationToken: cancellationToken);
                Result<HierarchicalStep2LgdCalculationResult> step2Result = await lgdCalculationService.ExecuteStep2Async(
                    LgdCalculationType.LGD,
                    null,
                    null,
                    cancellationToken);

                if (!step2Result.IsSuccess)
                {
                    logger.LogWarning("Step 2 of LGD Calculation Pipeline failed: {Error}", step2Result.Error.Description);
                    throw new InvalidOperationException($"Step 2 failed: {step2Result.Error.Description}");
                }

                HierarchicalStep2LgdCalculationResult hierarchicalResult = step2Result.Value;
                logger.LogInformation("Step 2 of LGD Calculation Pipeline executed successfully.");
                await progressPublisher.PublishProgress(sessionId, 2, 1, LgdProgressStatus.Completed, cancellationToken: cancellationToken);

                #endregion

                #region Step 3 & 4 - VC-Point Determination (conditional)

                Dictionary<string, decimal> vcPointsByClassification;

                if (useProvidedVcPoints)
                {
                    vcPointsByClassification = request.VcPointsByClassification!;
                    logger.LogInformation("Using provided VC points for {Count} classifications, skipping Steps 3 and 4", vcPointsByClassification.Count);
                }
                else
                {
                    #region Step 3 - Yearly LGD Average

                    logger.LogInformation("Starting Step 3 - Yearly LGD Average Calculation");
                    await progressPublisher.PublishProgress(sessionId, 3, 1, LgdProgressStatus.InProgress, cancellationToken: cancellationToken);
                    Result<Step3YearlyLgdAverageResult> step3Result = await lgdCalculationService.ExecuteStep3Async(
                        hierarchicalResult,
                        cancellationToken);

                    if (!step3Result.IsSuccess)
                    {
                        logger.LogWarning("Step 3 of LGD Calculation Pipeline failed: {Error}", step3Result.Error.Description);
                        throw new InvalidOperationException($"Step 3 failed: {step3Result.Error.Description}");
                    }

                    logger.LogInformation("Step 3 of LGD Calculation Pipeline executed successfully.");
                    await progressPublisher.PublishProgress(sessionId, 3, 1, LgdProgressStatus.Completed, cancellationToken: cancellationToken);

                    #endregion

                    #region Step 4 - VC-Point Determination

                    logger.LogInformation("Starting Step 4 - VC-Point Determination");
                    await progressPublisher.PublishProgress(sessionId, 4, 1, LgdProgressStatus.InProgress, cancellationToken: cancellationToken);
                    Result<Step4VcPointDeterminationResult> step4ExecutionResult = await lgdCalculationService.ExecuteStep4Async(
                        step3Result.Value,
                        VcPointDeterminationMethod.MaxDeltaLgdMinusOne,
                        cancellationToken);

                    if (!step4ExecutionResult.IsSuccess)
                    {
                        logger.LogWarning("Step 4 of LGD Calculation Pipeline failed: {Error}", step4ExecutionResult.Error.Description);
                        throw new InvalidOperationException($"Step 4 failed: {step4ExecutionResult.Error.Description}");
                    }

                    Step4VcPointDeterminationResult step4Result = step4ExecutionResult.Value;
                    logger.LogInformation("Step 4 of LGD Calculation Pipeline executed successfully.");
                    await progressPublisher.PublishProgress(sessionId, 4, 1, LgdProgressStatus.Completed, cancellationToken: cancellationToken);

                    // Extract VC points by classification from Step 4 result
                    vcPointsByClassification = step4Result.ClassificationResults
                        .ToDictionary(vc => vc.Classification, vc => (decimal)vc.VcPoint);

                    logger.LogInformation("Extracted VC points for {Count} classifications", vcPointsByClassification.Count);

                    #endregion
                }

                #endregion

                #region Step 2 VC_LGD - Discounted Cashflow Summary with VC Points

                logger.LogInformation("Starting Step 2 VC_LGD - LGD Discounted Cashflow Summary with VC Points");
                await progressPublisher.PublishProgress(sessionId, 5, 1, LgdProgressStatus.InProgress, cancellationToken: cancellationToken);
                Result<HierarchicalStep2LgdCalculationResult> vcLgdStep2Result = await lgdCalculationService.ExecuteStep2Async(
                    LgdCalculationType.VC_LGD,
                    null,
                    vcPointsByClassification,
                    cancellationToken);

                if (!vcLgdStep2Result.IsSuccess)
                {
                    logger.LogWarning("Step 2 VC_LGD of LGD Calculation Pipeline failed: {Error}", vcLgdStep2Result.Error.Description);
                    throw new InvalidOperationException($"Step 2 VC_LGD failed: {vcLgdStep2Result.Error.Description}");
                }

                HierarchicalStep2LgdCalculationResult vcLgdHierarchicalResult = vcLgdStep2Result.Value;
                logger.LogInformation("Step 2 VC_LGD of LGD Calculation Pipeline executed successfully.");
                await progressPublisher.PublishProgress(sessionId, 5, 1, LgdProgressStatus.Completed, cancellationToken: cancellationToken);

                #endregion

                #region Step 5 LGD - Financial Year Analysis

                logger.LogInformation("Starting Step 5 LGD - Financial Year Analysis");
                await progressPublisher.PublishProgress(sessionId, 6, 1, LgdProgressStatus.InProgress, cancellationToken: cancellationToken);
                Result<Step5FinancialYearLgdResult> lgdStep5Result = await lgdCalculationService.ExecuteStep5Async(
                    hierarchicalResult,
                    financialYearEnds,
                    cancellationToken);

                if (!lgdStep5Result.IsSuccess)
                {
                    logger.LogWarning("Step 5 LGD of LGD Calculation Pipeline failed: {Error}", lgdStep5Result.Error.Description);
                    throw new InvalidOperationException($"Step 5 LGD failed: {lgdStep5Result.Error.Description}");
                }

                logger.LogInformation("Step 5 LGD of LGD Calculation Pipeline executed successfully.");
                await progressPublisher.PublishProgress(sessionId, 6, 1, LgdProgressStatus.Completed, cancellationToken: cancellationToken);

                #endregion

                #region Step 5 VC_LGD - Financial Year Analysis

                logger.LogInformation("Starting Step 5 VC_LGD - Financial Year Analysis");
                Result<Step5FinancialYearLgdResult> vcLgdStep5Result = await lgdCalculationService.ExecuteStep5Async(
                    vcLgdHierarchicalResult,
                    financialYearEnds,
                    cancellationToken);

                if (!vcLgdStep5Result.IsSuccess)
                {
                    logger.LogWarning("Step 5 VC_LGD of LGD Calculation Pipeline failed: {Error}", vcLgdStep5Result.Error.Description);
                    throw new InvalidOperationException($"Step 5 VC_LGD failed: {vcLgdStep5Result.Error.Description}");
                }

                logger.LogInformation("Step 5 VC_LGD of LGD Calculation Pipeline executed successfully.");

                #endregion

                #region Step 6 - Final Result Combination

                logger.LogInformation("Starting Step 6 - Final Result Combination");
                await progressPublisher.PublishProgress(sessionId, 7, 1, LgdProgressStatus.InProgress, cancellationToken: cancellationToken);
                Result<Step5FinancialYearLgdResult> step6Result = await lgdCalculationService.ExecuteStep6Async(
                    lgdStep5Result.Value,
                    vcLgdStep5Result.Value,
                    cancellationToken);

                if (!step6Result.IsSuccess)
                {
                    logger.LogWarning("Step 6 of LGD Calculation Pipeline failed: {Error}", step6Result.Error.Description);
                    throw new InvalidOperationException($"Step 6 failed: {step6Result.Error.Description}");
                }

                logger.LogInformation("Step 6 of LGD Calculation Pipeline executed successfully.");
                await progressPublisher.PublishProgress(sessionId, 7, 1, LgdProgressStatus.Completed, cancellationToken: cancellationToken);

                #endregion

                logger.LogInformation("✅ LGD Calculation Pipeline completed successfully. All steps (1-6) executed without error. Session ID: {SessionId}", sessionId);

                // Create response with sessionId for tracking
                var response = new
                {
                    SessionId = sessionId,
                    Result = step6Result.Value,
                    Message = "LGD Calculation Pipeline completed successfully"
                };

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while executing LGD Calculation pipeline");
                return Results.Problem(new ProblemDetails
                {
                    Title = "LGD Calculation Pipeline Execution Failed",
                    Detail = "An error occurred while executing the LGD Calculation pipeline. Please check the logs for more details.",
                    Status = 500,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
                });
            }
        })
        .WithTags("LGD Calculation")
        .WithName("ExecuteLgdCalculationPipeline")
        .WithSummary("Execute LGD Calculation Pipeline")
        .WithDescription("Executes the complete LGD Calculation pipeline, running steps 1-6: LGD data preparation, discounted cashflow summary, yearly LGD average calculation, VC-point determination (optional if provided), VC_LGD processing, financial year analysis, and final result combination.")
        .Accepts<LgdCalculationPipelineRequest>("application/json")
        .Produces<Step5FinancialYearLgdResult>(200)
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