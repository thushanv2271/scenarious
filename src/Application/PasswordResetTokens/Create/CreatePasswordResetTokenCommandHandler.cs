using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.PasswordResetTokens;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.PasswordResetTokens.Create;

internal sealed class CreatePasswordResetTokenCommandHandler(
    IApplicationDbContext context)
    : ICommandHandler<CreatePasswordResetTokenCommand, string>
{
    public async Task<Result<string>> Handle(CreatePasswordResetTokenCommand command, CancellationToken cancellationToken)
    {
        // Optional: Check for existing active token (not expired, not used)
        PasswordResetToken? existingToken = await context.PasswordResetTokens
            .FirstOrDefaultAsync(
                x => x.Email == command.Email &&
                     !x.IsUsed &&
                     x.ExpiresAt > DateTime.UtcNow,
                cancellationToken
            );

        if (existingToken is not null)
        {
            return Result.Failure<string>(PasswordResetTokenErrors.AlreadyExists());
        }

        string token = Guid.CreateVersion7().ToString("N");

        DateTime expiresAt = DateTime.UtcNow.AddMinutes(15);

        var resetToken = new PasswordResetToken
        {
            Email = command.Email,
            Token = token,
            ExpiresAt = expiresAt,
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        await context.PasswordResetTokens.AddAsync(resetToken, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(token);
    }
}
