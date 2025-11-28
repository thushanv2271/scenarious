// src/Application/EclAnalysis/CalculateThresholdSummary/CalculateEclThresholdSummaryCommandValidator.cs
using Domain.EclAnalysis;
using FluentValidation;

namespace Application.EclAnalysis.CalculateThresholdSummary;

/// <summary>
/// Validates the ECL threshold summary calculation command
/// </summary>
internal sealed class CalculateEclThresholdSummaryCommandValidator : AbstractValidator<CalculateEclThresholdSummaryCommand>
{
    public CalculateEclThresholdSummaryCommandValidator()
    {
        // User ID cannot be empty
        RuleFor(c => c.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        // Threshold type must be valid
        RuleFor(c => c.ThresholdType)
            .IsInEnum()
            .WithMessage("Invalid threshold type.");

        // Custom Absolute validation
        When(c => c.ThresholdType == ThresholdType.CustomAbsolute, () => RuleFor(c => c.IndividualSignificantThreshold)
                .NotNull()
                .WithMessage("Individual significant threshold is required for CustomAbsolute type.")
                .GreaterThan(0)
                .WithMessage("Individual significant threshold must be greater than zero."));

        // Top N Customers validation
        When(c => c.ThresholdType == ThresholdType.TopNCustomers, () => RuleFor(c => c.TopNCount)
                .NotNull()
                .WithMessage("Top N count is required for TopNCustomers type.")
                .GreaterThan(0)
                .WithMessage("Top N count must be greater than zero."));

        // Cumulative Percentage validation
        When(c => c.ThresholdType == ThresholdType.CumulativePercentage, () => RuleFor(c => c.CumulativePercentageThreshold)
                .NotNull()
                .WithMessage("Cumulative percentage threshold is required for CumulativePercentage type.")
                .GreaterThan(0)
                .WithMessage("Cumulative percentage threshold must be greater than zero.")
                .LessThanOrEqualTo(100)
                .WithMessage("Cumulative percentage threshold cannot exceed 100."));
    }
}
