using Application.Abstractions.Messaging;
using Application.Roles.RemovePermissions;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Roles;

internal sealed class RemovePermissions : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapPut("roles/{roleId:guid}/permissions/remove", async (
			Guid roleId,
			RemovePermissionsRequest request,
			ICommandHandler<RemovePermissionsCommand> handler,
			CancellationToken cancellationToken) =>
		{
			var command = new RemovePermissionsCommand(roleId, request.PermissionKeys);
			Result result = await handler.Handle(command, cancellationToken);

			return result.Match(
				() => Results.NoContent(),
				CustomResults.Problem);
		})
		.RequireAuthorization()
		.HasPermission(PermissionRegistry.AdminSettingsRolePermissionEdit)
		.WithTags(Tags.Roles);

	}
}

public sealed record RemovePermissionsRequest(IReadOnlyList<string> PermissionKeys);
