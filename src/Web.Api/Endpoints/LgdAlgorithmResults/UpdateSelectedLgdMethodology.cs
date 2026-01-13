using Application.Abstractions.Messaging;
using Application.LgdAlgorithmResults.UpdateSelectedMethodology;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LgdAlgorithmResults;

/// <summary>
/// Endpoint to update the selected methodology for a specific product category and segment in LGD Algorithm Results
/// </summary>
internal sealed class UpdateSelectedLgdMethodology : IEndpoint
{
    public sealed record UpdateSelectedLgdMethodologyRequest(
        string ProductCategory,
        string Segment,
        string SelectedMethodology
    );

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch("lgd-algorithm-results/{id:guid}/selected-methodology", async (
            Guid id,
            UpdateSelectedLgdMethodologyRequest request,
            ICommandHandler<UpdateSelectedLgdMethodologyCommand, UpdateSelectedLgdMethodologyResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateSelectedLgdMethodologyCommand(
                id,
                request.ProductCategory,
                request.Segment,
                request.SelectedMethodology);

            Result<UpdateSelectedLgdMethodologyResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.LGDSetupAccess)
        .WithTags(Tags.LgdAlgorithmResults)
        .WithName("UpdateSelectedLgdMethodology")
        .WithDescription("Updates the selected methodology for a specific product category and segment in LGD Algorithm Results")
        .Produces<UpdateSelectedLgdMethodologyResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);
    }
}