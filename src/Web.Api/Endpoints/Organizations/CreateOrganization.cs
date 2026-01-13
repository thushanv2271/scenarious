using Application.Abstractions.Messaging;
using Application.Organizations.CreateOrganization;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Organizations;

/// <summary>
/// Endpoint for creating a new organization        
/// </summary>
internal sealed class Create : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Map the POST endpoint for creating an organization
        app.MapPost("organizations", async (
            CreateOrganizationRequest request,
            ICommandHandler<CreateOrganizationCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateOrganizationCommand(
                request.Name,
                request.Code,
                request.Email,
                request.ContactNumber,
                request.Address,
                request.IsActive,
                request.FinancialYearEnd
            );

            // Handle the command to create the organization
            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                id => Results.Created($"organizations/{id}", new { Id = id }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithName("CreateOrganization")
        .WithSummary("Create a new organization")
        .WithDescription("Creates a new organization with the provided details")
        .WithTags("Organizations")
        .WithOpenApi();
    }
}

internal sealed record CreateOrganizationRequest(
    string Name,
    string Code,
    string Email,
    string ContactNumber,
    string Address,
    bool IsActive = true,
    DateOnly? FinancialYearEnd = null
);
