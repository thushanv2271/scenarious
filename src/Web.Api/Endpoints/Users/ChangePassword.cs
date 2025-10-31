using System.Security.Claims;
using Application.Abstractions.Messaging;
using Application.Users.ChangePassword;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class ChangePasswordEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/change-password", async (
            HttpContext httpContext,
            ICommandHandler<ChangePasswordCommand, string> handler,
            Request request,
            CancellationToken cancellationToken) =>
        {
            // Extract UserId from token claims
            string? userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                // Use Result.Failure so CustomResults.Problem can accept it
                var failureResult = Result.Failure(new Error(
                    "InvalidToken",
                    "Invalid token: UserId not found",
                    ErrorType.Validation
                ));
                return CustomResults.Problem(failureResult);
            }

            var command = new ChangePasswordCommand(
                userId,
                request.CurrentPassword,
                request.NewPassword,
                request.ConfirmPassword
            );

            Result<string> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.UsersAccess)
        .WithTags(Tags.Users);
    }

    public sealed record Request(
        string CurrentPassword,
        string NewPassword,
        string ConfirmPassword
    );
}
