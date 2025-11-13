using Domain.FacilityCashFlowTypes;
using FluentValidation;

namespace Application.FacilityCashFlowTypes.SaveCashFlowType;

/// <summary>
/// Validator for SaveFacilityCashFlowTypeCommand
/// </summary>
internal sealed class SaveFacilityCashFlowTypeCommandValidator
    : AbstractValidator<SaveFacilityCashFlowTypeCommand>
{
    public SaveFacilityCashFlowTypeCommandValidator()
    {
        RuleFor(x => x.FacilityNumber)
            .NotEmpty()
            .WithMessage("Facility number is required")
            .MaximumLength(50)
            .WithMessage("Facility number cannot exceed 50 characters");

        RuleFor(x => x.SegmentId)
            .NotEmpty()
            .WithMessage("Segment ID is required");

        RuleFor(x => x.ScenarioId)
            .NotEmpty()
            .WithMessage("Scenario ID is required");

        RuleFor(x => x.CashFlowType)
            .IsInEnum()
            .WithMessage("Invalid cash flow type");

        RuleFor(x => x.Configuration)
            .NotNull()
            .WithMessage("Configuration is required");
    }
}
