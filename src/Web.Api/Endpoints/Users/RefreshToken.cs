using Application.Users.RefreshTokens;
using SharedKernel;
using Web.Api.Infrastructure;
using Web.Api.Extensions;
using Application.Abstractions.Messaging;
using Application.Users.Login;


namespace Web.Api.Endpoints.Users;

internal sealed class RefreshToken : IEndpoint
{
	public sealed record RefreshTokenRequest(string RefreshToken);

	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapPost("users/refresh-token", async (
			RefreshTokenRequest request,
			ICommandHandler<RefreshTokensCommand, (string AccessToken, string RefreshToken)> handler,
			CancellationToken cancellationToken) =>
		{
			var command = new RefreshTokensCommand(request.RefreshToken);
			Result<(string AccessToken, string RefreshToken)> result = await handler.Handle(command, cancellationToken);

			return result.Match(
				value => Results.Ok(new
				{
					value.AccessToken,
					value.RefreshToken
				}),
				CustomResults.Problem
			);
		})
		.RequireAuthorization()
		.HasPermission(PermissionRegistry.UsersAccess)
		.WithTags(Tags.Users);
	}
}
