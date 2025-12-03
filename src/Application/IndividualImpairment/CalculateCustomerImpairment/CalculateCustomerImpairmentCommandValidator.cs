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
            .MaximumLength(50);

        RuleFor(x => x.Facilities)
            .NotEmpty()
            .WithMessage("At least one facility is required");

        RuleForEach(x => x.Facilities)
            .ChildRules(facility =>
            {
                facility.RuleFor(f => f.FacilityNumber)
                    .NotEmpty()
                    .WithMessage("Facility number is required");

                facility.RuleFor(f => f.AmortizedCost)
                    .GreaterThan(0)
                    .WithMessage("Amortized cost must be greater than zero");

                facility.RuleFor(f => f.InterestRate)
                    .InclusiveBetween(0, 1)
                    .WithMessage("Interest rate must be between 0 and 1");

                facility.RuleFor(f => f.Scenarios)
                    .NotEmpty()
                    .WithMessage("At least one scenario is required per facility");
            });
    }
}
