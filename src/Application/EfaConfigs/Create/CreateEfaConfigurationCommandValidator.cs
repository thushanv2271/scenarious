using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Abstractions.Messaging;
using FluentValidation;

namespace Application.EfaConfigs.Create;

/// <summary>
/// Ensures that the command contains valid data before being processed,
/// such as required fields, valid year range, non-negative EFA rate,
/// and prevention of duplicate year entries.
/// </summary>
internal sealed class CreateEfaConfigurationCommandValidator
    : AbstractValidator<CreateEfaConfigurationCommand>
{
    /// <summary>
    /// Initializes validation rules/>.
    /// </summary>
    public CreateEfaConfigurationCommandValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("At least one EFA configuration item is required");

        RuleFor(x => x.UpdatedBy)
            .NotEmpty()
            .WithMessage("UpdatedBy is required");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.Year)
                .GreaterThan(1900)
                .LessThanOrEqualTo(2100)
                .WithMessage("Year must be between 1900 and 2100");

            item.RuleFor(x => x.EfaRate)
                .GreaterThanOrEqualTo(0)
                .WithMessage("EFA rate must be greater than or equal to 0");
        });

        // Ensures no duplicate years are included in the same request
        RuleFor(x => x.Items)
            .Must(items => items.Select(i => i.Year).Distinct().Count() == items.Count)
            .WithMessage("Duplicate years are not allowed in the same request");
    }
}
