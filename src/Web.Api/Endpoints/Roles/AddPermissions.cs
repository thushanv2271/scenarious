using Application.Abstractions.Messaging;
using Application.Roles.AddPermissions;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Roles;

internal sealed class AddPermissions : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapPut("roles/{roleId:guid}/permissions", async (
			Guid roleId,
			AddPermissionsRequest request,
			ICommandHandler<AddPermissionsCommand, Guid> handler,
			CancellationToken cancellationToken) =>
		{
			var command = new AddPermissionsCommand(roleId, request.PermissionKeys);
			Result<Guid> result = await handler.Handle(command, cancellationToken);

			return result.Match(
				_ => Results.Ok(roleId),
				CustomResults.Problem);
		})
		.RequireAuthorization()
		.HasPermission(PermissionRegistry.AdminSettingsRolePermissionEdit)
		.WithTags(Tags.Roles);
	}
}

public sealed record AddPermissionsRequest(IReadOnlyList<string> PermissionKeys);
