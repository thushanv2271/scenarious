using Application.Abstractions.Messaging;
using Application.Branches.Delete;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Branches;

/// <summary>
/// API endpoint for deleting branches
/// URL: DELETE /branches/{branchId}
/// Permission required: AdminSettingsRolePermissionDelete
/// Note: Cannot delete branch if it has users assigned
/// </summary>
internal sealed class Delete : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("branches/{branchId:guid}", async (
            Guid branchId,
            ICommandHandler<DeleteBranchCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteBranchCommand(branchId);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok("Branch deleted successfully"),
                CustomResults.Problem
            );
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.AdminSettingsRolePermissionDelete)
        .WithTags("Branches");
    }
}
