using FluentValidation;

namespace Application.IndividualImpairment.CalculateFacilityImpairment;

public sealed class CalculateFacilityImpairmentCommandValidator
    : AbstractValidator<CalculateFacilityImpairmentCommand>
{
    public CalculateFacilityImpairmentCommandValidator()
    {
        RuleFor(x => x.FacilityNumber)
            .NotEmpty()
            .WithMessage("Facility number is required")
            .MaximumLength(50)
            .WithMessage("Facility number cannot exceed 50 characters");

        When(x => x.Overrides != null, () =>
        {
            RuleFor(x => x.Overrides!.HaircutPercentage)
                .InclusiveBetween(0, 1)
                .When(x => x.Overrides!.HaircutPercentage.HasValue)
                .WithMessage("Haircut percentage must be between 0 and 1");

            RuleFor(x => x.Overrides!.AmortizedCost)
                .GreaterThan(0)
                .When(x => x.Overrides!.AmortizedCost.HasValue)
                .WithMessage("Amortized cost must be greater than zero");

            RuleFor(x => x.Overrides!.InterestRate)
                .InclusiveBetween(0, 1)
                .When(x => x.Overrides!.InterestRate.HasValue)
                .WithMessage("Interest rate must be between 0 and 1");
        });
    }
}
