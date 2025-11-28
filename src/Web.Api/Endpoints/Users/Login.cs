using Application.Abstractions.Messaging;
using Application.Users.Login;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class Login : IEndpoint
{
    public sealed record Request(string Email, string Password);

    public sealed record Response(string AccessToken, string RefreshToken, bool IsTemporaryPassword, bool IsWizardComplete, DateOnly? FinancialYearEnd);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/login", async (
            Request request,
            ICommandHandler<LoginUserCommand, (string AccessToken, string RefreshToken, bool IsTemporaryPassword, bool IsWizardComplete, DateOnly? FinancialYearEnd)> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new LoginUserCommand(request.Email, request.Password);

            Result<(string AccessToken, string RefreshToken, bool IsTemporaryPassword, bool IsWizardComplete, DateOnly? FinancialYearEnd)> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                token => Results.Ok(new Response(token.AccessToken, token.RefreshToken, token.IsTemporaryPassword, token.IsWizardComplete, token.FinancialYearEnd)),
                CustomResults.Problem
            );
        })
        .WithTags(Tags.Users);
    }
}
