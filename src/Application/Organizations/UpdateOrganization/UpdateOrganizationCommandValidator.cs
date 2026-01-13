using FluentValidation;

namespace Application.Organizations.UpdateOrganization;

public sealed class UpdateOrganizationCommandValidator : AbstractValidator<UpdateOrganizationCommand>
{
    public UpdateOrganizationCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Organization ID is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Organization name is required")
            .MaximumLength(200)
            .WithMessage("Organization name cannot exceed 200 characters");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Email must be a valid email address")
            .MaximumLength(100)
            .WithMessage("Email cannot exceed 100 characters");

        RuleFor(x => x.ContactNumber)
            .NotEmpty()
            .WithMessage("Contact number is required")
            .MaximumLength(20)
            .WithMessage("Contact number cannot exceed 20 characters");

        RuleFor(x => x.Address)
            .NotEmpty()
            .WithMessage("Address is required")
            .MaximumLength(500)
            .WithMessage("Address cannot exceed 500 characters");
    }
}
