using Application.Abstractions.Messaging;
using Application.Users.Register;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;
using Application.Users.ForgotPassword;

namespace Web.Api.Endpoints.Users;

internal sealed class Register : IEndpoint
{
    public sealed record RegisterRequest(string Email, string FirstName, string LastName, List<Guid> RoleIds, Guid? BranchId);
    public sealed record ForgotPasswordRequest(string Email);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/register", async (
            RegisterRequest request,
            ICommandHandler<RegisterUserCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RegisterUserCommand(
                request.Email,
                request.FirstName,
                request.LastName,
                request.RoleIds,
                request.BranchId);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                userId => Results.Ok(new { userId }),
                CustomResults.Problem
            );

        })
        .HasPermission(PermissionRegistry.AdminUserManagementCreate)
        .WithTags(Tags.Users);


        app.MapPost("users/forgot-password", async (
           ForgotPasswordRequest request,
           ICommandHandler<ForgotPasswordCommand, string> handler,
           CancellationToken cancellationToken) =>
       {
           var command = new ForgotPasswordCommand(request.Email);

           Result<string> result = await handler.Handle(command, cancellationToken);

           return result.Match(Results.Ok, CustomResults.Problem);
       })
       .WithTags(Tags.Users);
    }



}
