using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Application.Branches.Create;

/// <summary>
/// Validates CreateBranchCommand before processing
/// Checks that all required fields are present and valid
/// </summary>
internal sealed class CreateBranchCommandValidator : AbstractValidator<CreateBranchCommand>
{
    public CreateBranchCommandValidator()
    {
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.BranchName).NotEmpty().MaximumLength(150);
        RuleFor(c => c.BranchCode).NotEmpty().MaximumLength(50);
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(100);
        RuleFor(c => c.ContactNumber).NotEmpty().MaximumLength(20);
        RuleFor(c => c.Address).NotEmpty();
    }
}
