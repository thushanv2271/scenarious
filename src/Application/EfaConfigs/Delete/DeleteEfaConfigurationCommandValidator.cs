using FluentValidation;

namespace Application.EfaConfigs.Delete;

/// <summary>
/// Ensures that required fields are provided before processing the deletion.
/// </summary>
internal sealed class DeleteEfaConfigurationCommandValidator
    : AbstractValidator<DeleteEfaConfigurationCommand>
{
    public DeleteEfaConfigurationCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Id is required");

        RuleFor(x => x.DeletedBy)
            .NotEmpty()
            .WithMessage("DeletedBy is required");
    }
}
