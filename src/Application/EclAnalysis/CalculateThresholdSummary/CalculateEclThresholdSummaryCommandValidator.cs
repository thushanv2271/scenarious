using FluentValidation;

namespace Application.EclAnalysis.CalculateThresholdSummary;

/// <summary>
/// Validates the ECL threshold summary calculation command
/// </summary>
internal sealed class CalculateEclThresholdSummaryCommandValidator : AbstractValidator<CalculateEclThresholdSummaryCommand>
{
    public CalculateEclThresholdSummaryCommandValidator()
    {
        // Threshold must be a positive value
        RuleFor(c => c.IndividualSignificantThreshold)
            .GreaterThan(0)
            .WithMessage("Individual significant threshold must be greater than zero.");

        // User ID cannot be empty
        RuleFor(c => c.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");
    }
}
