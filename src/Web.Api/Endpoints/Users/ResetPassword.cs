using Application.Abstractions.Messaging;
using Application.Users.ResetPassword;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class ResetPassword : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapPost("users/reset-password", async (
			[FromBody] ResetPasswordCommand command,
			ICommandHandler<ResetPasswordCommand, string> handler,
			CancellationToken cancellationToken) =>
		{
			Result<string> result = await handler.Handle(command, cancellationToken);

			return result.Match(Results.Ok, CustomResults.Problem);
		})
		.WithTags(Tags.Users);
	}
}
