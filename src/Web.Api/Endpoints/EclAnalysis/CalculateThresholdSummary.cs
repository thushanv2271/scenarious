using System;
using System.Security.Claims;
using Application.Abstractions.Messaging;
using Application.EclAnalysis.CalculateThresholdSummary;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.EclAnalysis;

internal sealed class CalculateThresholdSummary : IEndpoint
{
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
            // Extract UserId from JWT token
            string? userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                var failureResult = Result.Failure<EclThresholdSummaryResponse>(new Error(
                    "InvalidToken",
                    "Invalid token: UserId not found",
                    ErrorType.Validation
                ));
                return CustomResults.Problem(failureResult);
            }

            var command = new CalculateEclThresholdSummaryCommand(
                request.IndividualSignificantThreshold,
                userId);

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
