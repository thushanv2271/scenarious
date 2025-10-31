using FluentValidation;

namespace Application.Scenarios.Create;

internal sealed class CreateScenarioCommandValidator : AbstractValidator<CreateScenarioCommand>
{
    public CreateScenarioCommandValidator()
    {
        RuleFor(x => x.ProductCategoryId)
            .NotEmpty()
            .WithMessage("Product category ID is required");

        RuleFor(x => x.SegmentId)
            .NotEmpty()
            .WithMessage("Segment ID is required");

        RuleFor(x => x.Scenarios)
            .NotEmpty()
            .WithMessage("At least one scenario is required");

        RuleForEach(x => x.Scenarios).ChildRules(scenario =>
        {
            scenario.RuleFor(s => s.ScenarioName)
                .NotEmpty()
                .MaximumLength(200)
                .WithMessage("Scenario name is required and must not exceed 200 characters");

            scenario.RuleFor(s => s.Probability)
                .InclusiveBetween(0, 100)
                .WithMessage("Probability must be between 0 and 100");
        });
    }
}
