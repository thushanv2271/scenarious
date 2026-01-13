using Application.Abstractions.Messaging;
using Application.Organizations;
using Application.Organizations.GetOrganization;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Organizations;

/// <summary>
/// Endpoint for retrieving an organization by ID           
/// </summary>
internal sealed class GetById : IEndpoint
{
    // Map the GET endpoint for retrieving an organization by ID
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("organizations/{id:guid}", async (
            Guid id,
            IQueryHandler<GetOrganizationQuery, OrganizationResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetOrganizationQuery(id);

            Result<OrganizationResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithName("GetOrganization")
        .WithSummary("Get organization by ID")
        .WithDescription("Returns a specific organization by its ID")
        .WithTags("Organizations")
        .WithOpenApi();
    }
}
