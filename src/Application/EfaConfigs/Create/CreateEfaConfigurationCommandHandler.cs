using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.EfaConfigs;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.EfaConfigs.Create;

/// <summary>
/// Command handler responsible for creating or updating EFA configurations.
/// If a configuration for a given year already exists, it updates the record.
/// Otherwise, it creates a new configuration.
/// </summary>
internal sealed class CreateEfaConfigurationCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateEfaConfigurationCommand, CreateEfaConfigurationResponse>
{
    /// <summary>
    /// Creates new EFA configurations or updates existing ones based on the provided years.
    /// </summary>

    public async Task<Result<CreateEfaConfigurationResponse>> Handle(
        CreateEfaConfigurationCommand command,
        CancellationToken cancellationToken)
    {
        List<EfaConfigurationSummary> created = new();
        List<EfaConfigurationSummary> updated = new();

        // Extract all requested years
        var years = command.Items.Select(i => i.Year).ToList();

        // Fetch any existing configurations for these years
        List<EfaConfiguration> existingConfigs = await context.EfaConfigurations
            .Where(e => years.Contains(e.Year))
            .ToListAsync(cancellationToken);

        // Map existing configurations by year for quick lookup
        var existingYears = existingConfigs.ToDictionary(e => e.Year);

        foreach (EfaConfigurationItem item in command.Items)
        {
            if (existingYears.TryGetValue(item.Year, out EfaConfiguration? existing))
            {
                // Update existing configuration
                existing.EfaRate = item.EfaRate;
                existing.UpdatedAt = dateTimeProvider.UtcNow;
                existing.UpdatedBy = command.UpdatedBy;

                updated.Add(new EfaConfigurationSummary(
                    existing.Id,
                    existing.Year,
                    existing.EfaRate,
                    existing.UpdatedAt,
                    existing.UpdatedBy
                ));
            }
            else
            {
                // Create new configuration
                EfaConfiguration newConfig = new()
                {
                    Id = Guid.CreateVersion7(),
                    Year = item.Year,
                    EfaRate = item.EfaRate,
                    UpdatedAt = dateTimeProvider.UtcNow,
                    UpdatedBy = command.UpdatedBy
                };

                // Raise domain event for the newly created configuration
                newConfig.Raise(new EfaConfigurationCreatedDomainEvent(newConfig.Id));
                context.EfaConfigurations.Add(newConfig);

                created.Add(new EfaConfigurationSummary(
                    newConfig.Id,
                    newConfig.Year,
                    newConfig.EfaRate,
                    newConfig.UpdatedAt,
                    newConfig.UpdatedBy
                ));
            }
        }

        // Persist changes
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateEfaConfigurationResponse(
            created,
            updated
        ));
    }
}
