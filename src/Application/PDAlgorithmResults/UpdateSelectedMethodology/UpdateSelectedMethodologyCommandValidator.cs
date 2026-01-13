using FluentValidation;

namespace Application.PDAlgorithmResults.UpdateSelectedMethodology;

/// <summary>
/// Validator for UpdateSelectedMethodologyCommand
/// Note: Methodology validation is performed in the handler against actual available methods in the database
/// </summary>
internal sealed class UpdateSelectedMethodologyCommandValidator
    : AbstractValidator<UpdateSelectedMethodologyCommand>
{
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
            .MaximumLength(50)
            .WithMessage("Selected methodology cannot exceed 50 characters");
    }
}
