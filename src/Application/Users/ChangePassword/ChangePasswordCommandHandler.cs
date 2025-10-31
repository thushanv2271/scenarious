using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Users.ChangePassword;

internal sealed class ChangePasswordCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher
) : ICommandHandler<ChangePasswordCommand, string>
{
    public async Task<Result<string>> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        // Ensure new password matches confirmation
        if (command.NewPassword != command.ConfirmPassword)
        {
            return Result.Failure<string>(UserErrors.PasswordsDoNotMatch);
        }

        // Fetch the user
        User? user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == command.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<string>(UserErrors.NotFound(command.UserId));
        }

        // Verify the current password
        if (!passwordHasher.Verify(command.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure<string>(UserErrors.InvalidCurrentPassword);
        }

        // Update password and mark IsTemporaryPassword = false
        user.PasswordHash = passwordHasher.Hash(command.NewPassword);
        user.IsTemporaryPassword = false;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success("Password changed successfully");
    }
}
