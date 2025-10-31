using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.PasswordResetTokens;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.PasswordResetTokens.ValidateResetToken;

internal sealed class ValidateResetTokenCommandHandler(
    IApplicationDbContext context)
    : ICommandHandler<ValidateResetTokenCommand, string>
{
    public async Task<Result<string>> Handle(ValidateResetTokenCommand command, CancellationToken cancellationToken)
    {
        PasswordResetToken? tokenEntity = await context.PasswordResetTokens
            .FirstOrDefaultAsync(
                x => x.Token == command.Token &&
                     !x.IsUsed &&
                     x.ExpiresAt > DateTime.UtcNow,
                cancellationToken
            );

        if (tokenEntity is null)
        {
            return Result.Failure<string>(PasswordResetTokenErrors.Invalid());
        }

        return Result.Success("Token is valid.");
    }
}
