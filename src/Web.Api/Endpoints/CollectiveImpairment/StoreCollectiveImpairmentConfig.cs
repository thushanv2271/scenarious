using Application.Abstractions.Messaging;
using Application.CollectiveImpairment.StoreConfiguration;
using Domain.CollectiveImpairment;
using SharedKernel;
using System.Security.Claims;
using System.Text.Json.Nodes;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.CollectiveImpairment;

internal sealed class StoreCollectiveImpairmentConfig : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("collective-impairment", async (
            StoreCollectiveImpairmentConfigRequest request,
            HttpContext httpContext,
            ICommandHandler<StoreCollectiveImpairmentConfigCommand> handler,
            CancellationToken cancellationToken) =>
        {
            string? userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                var failureResult = Result.Failure(new Error(
                    "InvalidToken",
                    "Invalid token: UserId not found",
                    ErrorType.Validation
                ));
                return CustomResults.Problem(failureResult);
            }

            StoreCollectiveImpairmentConfigCommand command = new(
                request.Parameter,
                request.ConfigJson,
                userId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                    () => Results.Ok(),
                    CustomResults.Problem
                );
        })
        .RequireAuthorization()
        .WithTags(Tags.CollectiveImpairment);
    }
}

internal sealed record StoreCollectiveImpairmentConfigRequest(
    ParameterType Parameter,
    JsonObject ConfigJson);
