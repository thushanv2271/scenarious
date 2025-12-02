using Application.IndividualImpairment.DTOs;
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
            .MaximumLength(50);

        RuleFor(x => x.CustomerNumber)
            .NotEmpty()
            .WithMessage("Customer number is required")
            .MaximumLength(50);

        RuleFor(x => x.AmortizedCost)
            .GreaterThan(0)
            .WithMessage("Amortized cost must be greater than zero");

        RuleFor(x => x.InterestRate)
            .InclusiveBetween(0, 1)
            .WithMessage("Interest rate must be between 0 and 1 (e.g., 0.10 for 10%)");

        RuleFor(x => x.Scenarios)
            .NotEmpty()
            .WithMessage("At least one scenario is required");

        RuleFor(x => x.Scenarios)
            .Must(HaveTotalProbabilityOfOne)
            .WithMessage("Scenario probabilities must sum to 1.00 (100%)")
            .When(x => x.Scenarios != null && x.Scenarios.Any());

        RuleForEach(x => x.Scenarios)
            .ChildRules(scenario =>
            {
                scenario.RuleFor(s => s.ScenarioId)
                    .NotEmpty()
                    .WithMessage("Scenario ID is required");

                scenario.RuleFor(s => s.ScenarioName)
                    .NotEmpty()
                    .WithMessage("Scenario name is required");

                scenario.RuleFor(s => s.Probability)
                    .InclusiveBetween(0, 1)
                    .WithMessage("Scenario probability must be between 0 and 1");

                scenario.RuleFor(s => s.CashFlows)
                    .NotEmpty()
                    .WithMessage("Each scenario must have at least one cash flow");

                scenario.RuleForEach(s => s.CashFlows)
                    .ChildRules(cashFlow =>
                    {
                        cashFlow.RuleFor(cf => cf.Month)
                            .GreaterThan(0)
                            .WithMessage("Cash flow month must be greater than zero");

                        cashFlow.RuleFor(cf => cf.CashFlowAmount)
                            .GreaterThan(0)
                            .WithMessage("Cash flow amount must be greater than zero");
                    });
            });
    }

    private bool HaveTotalProbabilityOfOne(List<ScenarioCashFlowInput> scenarios)
    {
        if (scenarios == null || !scenarios.Any())
        {
            return true;
        }

        decimal total = scenarios.Sum(s => s.Probability);

        // Allow small tolerance for floating point precision (0.01 = 1%)
        return Math.Abs(total - 1.0m) < 0.01m;
    }
}
