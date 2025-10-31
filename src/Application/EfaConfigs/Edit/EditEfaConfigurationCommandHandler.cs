using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.EfaConfigs;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.EfaConfigs.Edit;

/// <summary>
/// Handles updating an existing EFA configuration.
/// Validates existence, checks for year conflicts, updates the configuration,
/// and returns a summary of the updated record.
/// </summary>
internal sealed class EditEfaConfigurationCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<EditEfaConfigurationCommand, EditEfaConfigurationResponse>
{
    public async Task<Result<EditEfaConfigurationResponse>> Handle(
        EditEfaConfigurationCommand command,
        CancellationToken cancellationToken)
    {
        // Fetch the configuration by ID
        EfaConfiguration? efaConfig = await context.EfaConfigurations
            .FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken);

        if (efaConfig is null)
        {
            return Result.Failure<EditEfaConfigurationResponse>(
                EfaConfigurationErrors.NotFound(command.Id));
        }

        // Check if the new year conflicts with another record (only if year is changing)
        if (efaConfig.Year != command.Year)
        {
            bool yearExists = await context.EfaConfigurations
                .AnyAsync(e => e.Year == command.Year && e.Id != command.Id, cancellationToken);

            if (yearExists)
            {
                return Result.Failure<EditEfaConfigurationResponse>(
                    EfaConfigurationErrors.YearAlreadyExists(command.Year));
            }
        }

        // Update fields
        efaConfig.Year = command.Year;
        efaConfig.EfaRate = command.EfaRate;
        efaConfig.UpdatedAt = dateTimeProvider.UtcNow;
        efaConfig.UpdatedBy = command.UpdatedBy;

        // Persist changes
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new EditEfaConfigurationResponse(
            efaConfig.Id,
            efaConfig.Year,
            efaConfig.EfaRate,
            efaConfig.UpdatedAt,
            efaConfig.UpdatedBy
        ));
    }
}
