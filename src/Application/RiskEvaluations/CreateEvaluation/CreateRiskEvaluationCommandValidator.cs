using System;
using FluentValidation;

namespace Application.RiskEvaluations.CreateEvaluation;

// Validator for CreateRiskEvaluationCommand
internal sealed class CreateRiskEvaluationCommandValidator
    : AbstractValidator<CreateRiskEvaluationCommand>
{
    public CreateRiskEvaluationCommandValidator()
    {
        // Customer number required and max length
        RuleFor(x => x.CustomerNumber)
            .NotEmpty()
            .MaximumLength(50);

        // Evaluation date must be provided and valid
        RuleFor(x => x.EvaluationDate)
            .NotEmpty()
            .LessThanOrEqualTo(DateTime.Today.AddDays(1)); // Future +1 allowed

        // Must have at least one indicator entry
        RuleFor(x => x.IndicatorEvaluations)
            .NotEmpty()
            .WithMessage("At least one indicator evaluation is required");

        // Validate each indicator item
        RuleForEach(x => x.IndicatorEvaluations).ChildRules(item =>
        {
            // IndicatorId must be a valid GUID
            item.RuleFor(i => i.IndicatorId)
                .NotEmpty()
                .NotEqual(Guid.Empty)
                .WithMessage("IndicatorId must be a valid non-empty GUID.");

            // Allowed values: Yes / No / N/A
            item.RuleFor(i => i.Value)
                .NotEmpty()
                .Must(v => v == "Yes" || v == "No" || v == "N/A")
                .WithMessage("Value must be 'Yes', 'No', or 'N/A'");

        });
    }
}
