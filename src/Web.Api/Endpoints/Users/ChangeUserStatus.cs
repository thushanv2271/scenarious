using Application.Abstractions.Messaging;
using Application.Users.ChangeStatus;
using Domain.Users;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class ChangeUserStatus : IEndpoint
{
    public sealed record Request(Guid UserId, UserStatus NewStatus);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("users/status", async (
            Request request,
            ICommandHandler<ChangeUserStatusCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ChangeUserStatusCommand(request.UserId, request.NewStatus);
            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok(),
                error => CustomResults.Problem(error)
            );

        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.AdminUserManagementEdit)
        .WithTags(Tags.Users);
    }
}
