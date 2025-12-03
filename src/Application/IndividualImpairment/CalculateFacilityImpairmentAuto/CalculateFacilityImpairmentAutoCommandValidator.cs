using FluentValidation;

namespace Application.IndividualImpairment.CalculateFacilityImpairmentAuto;

internal sealed class CalculateFacilityImpairmentAutoCommandValidator
    : AbstractValidator<CalculateFacilityImpairmentAutoCommand>
{
    public CalculateFacilityImpairmentAutoCommandValidator()
    {
        RuleFor(x => x.FacilityNumber)
            .NotEmpty()
            .WithMessage("Facility number is required")
            .MaximumLength(50)
            .WithMessage("Facility number cannot exceed 50 characters");
    }
}
