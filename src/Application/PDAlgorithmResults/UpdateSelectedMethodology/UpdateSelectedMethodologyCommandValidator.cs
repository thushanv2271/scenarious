
using FluentValidation;

namespace Application.PDAlgorithmResults.UpdateSelectedMethodology;

/// <summary>
/// Validator for UpdateSelectedMethodologyCommand
/// </summary>
internal sealed class UpdateSelectedMethodologyCommandValidator
    : AbstractValidator<UpdateSelectedMethodologyCommand>
{
    private static readonly string[] ValidMethodologies = { "method1", "method2", "method3" };

    public UpdateSelectedMethodologyCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("PD Algorithm Result ID is required");

        RuleFor(x => x.ProductCategory)
            .NotEmpty()
            .WithMessage("Product category is required")
            .MaximumLength(100)
            .WithMessage("Product category cannot exceed 100 characters");

        RuleFor(x => x.Segment)
            .NotEmpty()
            .WithMessage("Segment is required")
            .MaximumLength(100)
            .WithMessage("Segment cannot exceed 100 characters");

        RuleFor(x => x.SelectedMethodology)
            .NotEmpty()
            .WithMessage("Selected methodology is required")
            .Must(BeValidMethodology)
            .WithMessage("Selected methodology must be one of: method1, method2, method3");
    }

    private static bool BeValidMethodology(string methodology)
    {
        return ValidMethodologies.Contains(methodology, StringComparer.OrdinalIgnoreCase);
    }
}
