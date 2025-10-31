using FluentValidation;

namespace Application.Users.Register;

internal sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(c => c.FirstName).NotEmpty();
        RuleFor(c => c.LastName).NotEmpty();
        RuleFor(c => c.Email).NotEmpty().EmailAddress();


        RuleFor(c => c.BranchId)
         .NotEmpty()
         .When(c => c.BranchId.HasValue)
         .WithMessage("Branch ID must be valid if provided");
    }
}
