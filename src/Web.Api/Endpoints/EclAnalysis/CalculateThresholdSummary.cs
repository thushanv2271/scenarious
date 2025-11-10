using System;
using System.Security.Claims;
using Application.Abstractions.Messaging;
using Application.EclAnalysis.CalculateThresholdSummary;
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
    /// Request payload containing threshold value
    /// </summary>
    public sealed record ThresholdSummaryRequest(
        decimal IndividualSignificantThreshold);

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

            // Create command with threshold and authenticated user ID
            var command = new CalculateEclThresholdSummaryCommand(
                request.IndividualSignificantThreshold,
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
