using Application.Abstractions.Messaging;
using Application.Organizations.UpdateOrganization;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Organizations;

/// <summary>
/// Endpoint for updating an organization   
/// </summary>
internal sealed class Update : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Map the PUT endpoint for updating an organization by ID
        app.MapPut("organizations/{id:guid}", async (
            Guid id,
            UpdateOrganizationRequest request,
            ICommandHandler<UpdateOrganizationCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateOrganizationCommand(
                id,
                request.Name,
                request.Email,
                request.ContactNumber,
                request.Address,
                request.IsActive,
                request.FinancialYearEnd
            );

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok(new { Message = "Organization updated successfully" }),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithName("UpdateOrganization")
        .WithSummary("Update an organization")
        .WithDescription("Updates an existing organization with the provided details")
        .WithTags("Organizations")
        .WithOpenApi();
    }
}

internal sealed record UpdateOrganizationRequest(
    string Name,
    string Email,
    string ContactNumber,
    string Address,
    bool IsActive,
    DateOnly? FinancialYearEnd
);
