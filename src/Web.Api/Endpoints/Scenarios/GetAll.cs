using Application.Abstractions.Messaging;
using Application.Scenarios.GetAll;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Scenarios;

/// <summary>
/// Endpoint to retrieve scenarios grouped by product category and segment
/// </summary>
internal sealed class GetAll : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("scenarios", async (
            Guid? productCategoryId,
            Guid? segmentId,
            IQueryHandler<GetAllScenariosQuery, List<ProductCategoryWithScenariosResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAllScenariosQuery(productCategoryId, segmentId);

            Result<List<ProductCategoryWithScenariosResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("Scenarios")
        .WithName("GetAllScenarios")
        .Produces<List<ProductCategoryWithScenariosResponse>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
