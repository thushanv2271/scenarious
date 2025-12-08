using Application.Abstractions.Messaging;
using Application.PDAlgorithmResults.UpdateSelectedMethodology;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.PDAlgorithmResults;

/// <summary>
/// Endpoint to update the selected methodology for a specific product category and segment
/// </summary>
internal sealed class UpdateSelectedMethodology : IEndpoint
{
    public sealed record UpdateSelectedMethodologyRequest(
        string ProductCategory,
        string Segment,
        string SelectedMethodology
    );

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch("pd-algorithm-results/{id:guid}/selected-methodology", async (
            Guid id,
            UpdateSelectedMethodologyRequest request,
            ICommandHandler<UpdateSelectedMethodologyCommand, UpdateSelectedMethodologyResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateSelectedMethodologyCommand(
                id,
                request.ProductCategory,
                request.Segment,
                request.SelectedMethodology);

            Result<UpdateSelectedMethodologyResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags(Tags.PDAlgorithmResults)
        .WithName("UpdateSelectedMethodology")
        .Produces<UpdateSelectedMethodologyResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);
    }
}
