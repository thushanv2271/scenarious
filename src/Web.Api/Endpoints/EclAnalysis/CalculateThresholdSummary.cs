// src/Web.Api/Endpoints/EclAnalysis/CalculateThresholdSummary.cs
using System;
using System.Security.Claims;
using Application.Abstractions.Messaging;
using Application.EclAnalysis.CalculateThresholdSummary;
using Domain.EclAnalysis;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.EclAnalysis;

/// <summary>
/// API endpoint for calculating ECL threshold summary
/// </summary>
internal sealed class CalculateThresholdSummary : IEndpoint
{
    /// <summary>
    /// Request payload containing threshold configuration
    /// </summary>
    public sealed record ThresholdSummaryRequest(
        string ThresholdType,
        decimal? IndividualSignificantThreshold = null,
        int? TopNCount = null,
        decimal? CumulativePercentageThreshold = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/impairment/ecl/threshold-summary", async (
            [FromBody] ThresholdSummaryRequest request,
            HttpContext httpContext,
            ICommandHandler<CalculateEclThresholdSummaryCommand, EclThresholdSummaryResponse> handler,
            CancellationToken cancellationToken) =>
        {
            // Extract user ID from JWT claims
            string? userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Validate user ID exists and is a valid GUID
            if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                var failureResult = Result.Failure<EclThresholdSummaryResponse>(new Error(
                    "InvalidToken",
                    "Invalid token: UserId not found",
                    ErrorType.Validation
                ));
                return CustomResults.Problem(failureResult);
            }

            // Parse threshold type
            if (!Enum.TryParse<ThresholdType>(request.ThresholdType, true, out ThresholdType thresholdType))
            {
                var failureResult = Result.Failure<EclThresholdSummaryResponse>(
                    EclAnalysisErrors.InvalidThresholdType);
                return CustomResults.Problem(failureResult);
            }

            // Create command with threshold configuration and authenticated user ID
            var command = new CalculateEclThresholdSummaryCommand(
                thresholdType,
                request.IndividualSignificantThreshold,
                request.TopNCount,
                request.CumulativePercentageThreshold,
                userId);

            // Execute command and return result
            Result<EclThresholdSummaryResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                response => Results.Ok(response),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.EclAnalysisThresholdCalculation)
        .WithTags(Tags.EclAnalysis);
    }
}
