using Application.Abstractions.Messaging;
using Application.Organizations;
using Application.Organizations.GetOrganizations;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Organizations;

/// <summary>
/// Endpoint for retrieving all organizations with optional filtering
/// </summary>
internal sealed class GetAll : IEndpoint
{
    /// <summary>
    /// Maps the GET endpoint for retrieving organizations
    /// </summary>
    /// <param name="app">The endpoint route builder</param>
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("organizations", async (
            bool? isActive,
            string? searchTerm,
            IQueryHandler<GetOrganizationsQuery, List<OrganizationResponse>> handler,
            CancellationToken cancellationToken) =>
        {
             // Create the query object with the provided filters
            var query = new GetOrganizationsQuery(isActive, searchTerm);

             // Execute the query through the handler
            Result<List<OrganizationResponse>> result = await handler.Handle(query, cancellationToken);

             // Return OK response with organizations if successful, or problem details if failed
            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithName("GetOrganizations")
        .WithSummary("Get all organizations")
        .WithDescription("Returns all organizations with optional filtering")
        .WithTags("Organizations")
        .WithOpenApi();
    }
}
