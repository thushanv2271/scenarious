using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.Login;

internal sealed class LoginUserCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    ITokenProvider tokenProvider,
    IRefreshTokenService refreshTokenService)
    : ICommandHandler<LoginUserCommand, (string AccessToken, string RefreshToken, bool IsTemporaryPassword, bool IsWizardComplete)>
{
    public async Task<Result<(string AccessToken, string RefreshToken, bool IsTemporaryPassword, bool IsWizardComplete)>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        User? user = await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email == command.Email, cancellationToken);

        if (user is null)
        {
            return Result.Failure<(string, string, bool, bool)>(UserErrors.NotFoundByEmail);
        }
        if (!passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            return Result.Failure<(string, string, bool, bool)>(UserErrors.InvalidPassword);
        }

        string accessToken = await tokenProvider.CreateAsync(user);
        string refreshToken = await refreshTokenService.CreateRefreshTokenAsync(user, cancellationToken);

        return (accessToken, refreshToken, user.IsTemporaryPassword, user.IsWizardComplete);
    }
}
