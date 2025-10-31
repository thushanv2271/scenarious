using FluentValidation;

namespace Application.PasswordResetTokens.ValidateResetToken;

public sealed class ValidateResetTokenCommandValidator : AbstractValidator<ValidateResetTokenCommand>
{
	public ValidateResetTokenCommandValidator()
	{
		RuleFor(x => x.Token)
			.NotEmpty().WithMessage("Token must not be empty")
			.Length(10, 100).WithMessage("Token must be between 10 and 100 characters");
	}
}
