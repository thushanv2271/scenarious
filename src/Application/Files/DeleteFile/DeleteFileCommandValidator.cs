using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Application.Files.DeleteFile;
/// <summary>
/// Validates the DeleteFileCommand to ensure required fields are provided.
/// </summary>
internal sealed class DeleteFileCommandValidator : AbstractValidator<DeleteFileCommand>
{
    public DeleteFileCommandValidator()
    {
        RuleFor(x => x.Ids)
            .NotNull()
            .WithMessage("File IDs list cannot be null")
            .NotEmpty()
            .WithMessage("At least one file ID is required")
            .Must(ids => ids.All(id => id != Guid.Empty))
            .WithMessage("All file IDs must be valid (non-empty GUIDs)");

        RuleFor(x => x.DeletedBy)
            .NotEmpty()
            .WithMessage("DeletedBy user ID is required");
    }
}
