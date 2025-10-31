
using Application.Abstractions.Messaging;
using Application.Users.Register;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;
using Application.Users.ForgotPassword;
using Domain.Users;
using Application.Users.Update;

namespace Web.Api.Endpoints.Users;

internal sealed class Update : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("users", async (
            UpdateUserRequest request,
            ICommandHandler<UpdateUserCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateUserCommand(
                request.UserId,
                request.FirstName,
                request.LastName,
                request.UserStatus,
                request.RoleIds,
                 request.BranchId
            );

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Ok("User Updated successfully"),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.AdminUserManagementEdit)
        .WithTags(Tags.Users);
    }
}

public sealed record UpdateUserRequest(
    Guid UserId,
    string FirstName,
    string LastName,
    UserStatus UserStatus,
    List<Guid> RoleIds,
    Guid? BranchId  
);


