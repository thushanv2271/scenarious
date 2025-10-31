using SharedKernel;

namespace Domain.PasswordResetTokens;

public static class PasswordResetTokenErrors
{
	public static Error AlreadyExists() => Error.Conflict(
		  "PasswordResetToken.AlreadyExists",
		  "A valid reset token already exists for this email.");

	public static Error Invalid() => Error.Conflict(
		  "PasswordResetToken.Invalid",
		  "Provided Token is invalid");
}
