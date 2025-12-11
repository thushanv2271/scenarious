using FluentValidation;

namespace Application.IndividualImpairment.CalculateCustomerImpairment;

public sealed class CalculateCustomerImpairmentCommandValidator
    : AbstractValidator<CalculateCustomerImpairmentCommand>
{
    public CalculateCustomerImpairmentCommandValidator()
    {
        RuleFor(x => x.CustomerNumber)
            .NotEmpty()
            .WithMessage("Customer number is required")
            .MaximumLength(50)
            .WithMessage("Customer number cannot exceed 50 characters");

        When(x => x.Facilities != null && x.Facilities.Any(), () => RuleForEach(x => x.Facilities)
                .ChildRules(facility =>
                {
                    facility.RuleFor(f => f.FacilityNumber)
                        .NotEmpty()
                        .WithMessage("Facility number is required")
                        .MaximumLength(50)
                        .WithMessage("Facility number cannot exceed 50 characters");

                    facility.When(f => f.Overrides != null, () =>
                    {
                        facility.RuleFor(f => f.Overrides!.HaircutPercentage)
                            .InclusiveBetween(0, 1)
                            .When(f => f.Overrides!.HaircutPercentage.HasValue)
                            .WithMessage("Haircut percentage must be between 0 and 1");

                        facility.RuleFor(f => f.Overrides!.AmortizedCost)
                            .GreaterThan(0)
                            .When(f => f.Overrides!.AmortizedCost.HasValue)
                            .WithMessage("Amortized cost must be greater than zero");

                        facility.RuleFor(f => f.Overrides!.InterestRate)
                            .InclusiveBetween(0, 1)
                            .When(f => f.Overrides!.InterestRate.HasValue)
                            .WithMessage("Interest rate must be between 0 and 1");
                    });
                }));
    }
}
