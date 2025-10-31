using Application.Abstractions.Messaging;
using Application.Branches.Create;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Branches;

/// <summary>
/// API endpoint for creating new branches
/// URL: POST /branches
/// Permission required: AdminSettingsRolePermissionCreate
/// </summary>
internal sealed class Create : IEndpoint
{
    public sealed record CreateBranchRequest(
        Guid OrganizationId,
        string BranchName,
        string BranchCode,
        string Email,
        string ContactNumber,
        string Address
    );

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Register the POST endpoint at /branches
        app.MapPost("branches", async (
            CreateBranchRequest request,
            ICommandHandler<CreateBranchCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            //Convert request to command
            var command = new CreateBranchCommand(
                request.OrganizationId,
                request.BranchName,
                request.BranchCode,
                request.Email,
                request.ContactNumber,
                request.Address
            );

            //Execute the command
            Result<Guid> result = await handler.Handle(command, cancellationToken);

            // If successful: Return new branch ID
            // If failed: Return error details
            return result.Match(
                branchId => Results.Ok(new { branchId }),
                CustomResults.Problem
            );
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.AdminSettingsRolePermissionCreate)
        .WithTags("Branches");
    }
}
