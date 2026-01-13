using Application.Abstractions.Messaging;
using Application.LgdAlgorithmResults.UpdateLgdAlgorithmResult;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LgdAlgorithmResults;

/// <summary>
/// Endpoint to update the LGD Algorithm Result JSON data
/// </summary>
internal sealed class UpdateLgdAlgorithmResult : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("lgd-algorithm-results", async (
            UpdateLgdAlgorithmResultRequest request,
            ICommandHandler<UpdateLgdAlgorithmResultCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateLgdAlgorithmResultCommand(request.LgdAlgorithmResultData);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                id => Results.Ok(new { Id = id, Message = "LGD Algorithm Result updated successfully" }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.LGDSetupAccess)
        .WithTags(Tags.LgdAlgorithmResults)
        .WithName("UpdateLgdAlgorithmResult")
        .WithDescription("Updates the LGD Algorithm Result JSON data")
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);
    }
}