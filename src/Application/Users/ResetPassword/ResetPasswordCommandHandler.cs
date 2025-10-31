using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.PasswordResetTokens;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.ResetPassword;

internal sealed class ResetPasswordCommandHandler(
	IApplicationDbContext context,
	IPasswordHasher passwordHasher)
	: ICommandHandler<ResetPasswordCommand, string>
{
	public async Task<Result<string>> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
	{
		if (command.Password != command.ConfirmPassword)
		{
			return Result.Failure<string>(UserErrors.InvalidOrExpiredResetToken);
		}

		// Find the token and ensure it's valid
		PasswordResetToken? token = await context.PasswordResetTokens
			.FirstOrDefaultAsync(
				t => t.Token == command.Token &&
					 !t.IsUsed &&
					 t.ExpiresAt > DateTime.UtcNow,
				cancellationToken);

		if (token is null)
		{
			return Result.Failure<string>(UserErrors.InvalidOrExpiredResetToken);
		}

		// Extract email from token and find the corresponding user
		User? user = await context.Users
			.FirstOrDefaultAsync(u => u.Email == token.Email, cancellationToken);

		if (user is null)
		{
			return Result.Failure<string>(UserErrors.NotFoundByEmail);
		}

		// Update password and mark token as used
		user.PasswordHash = passwordHasher.Hash(command.Password);
		token.IsUsed = true;

		await context.SaveChangesAsync(cancellationToken);

		return Result.Success("Password reset successful");
	}
}

