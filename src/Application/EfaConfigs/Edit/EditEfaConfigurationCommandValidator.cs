using FluentValidation;

namespace Application.EfaConfigs.Edit;

/// <summary>
/// Ensures that all required fields are provided and valid before processing an update.
/// </summary>
internal sealed class EditEfaConfigurationCommandValidator
    : AbstractValidator<EditEfaConfigurationCommand>
{
    public EditEfaConfigurationCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Id is required");

        RuleFor(x => x.Year)
            .GreaterThan(1900)
            .LessThanOrEqualTo(2100)
            .WithMessage("Year must be between 1900 and 2100");

        RuleFor(x => x.EfaRate)
            .GreaterThanOrEqualTo(0)
            .WithMessage("EFA rate must be greater than or equal to 0");

        RuleFor(x => x.UpdatedBy)
            .NotEmpty()
            .WithMessage("UpdatedBy is required");
    }
}
