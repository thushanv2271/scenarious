using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.EfaConfigs;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.EfaConfigs.Delete;

/// <summary>
/// Handles the deletion of an existing EFA configuration.
/// Validates existence, removes the configuration, and returns a summary of the deleted item.
/// </summary>
internal sealed class DeleteEfaConfigurationCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<DeleteEfaConfigurationCommand, DeleteEfaConfigurationResponse>
{

    public async Task<Result<DeleteEfaConfigurationResponse>> Handle(
        DeleteEfaConfigurationCommand command,
        CancellationToken cancellationToken)
    {
        // Fetch the configuration by ID
        EfaConfiguration? efaConfig = await context.EfaConfigurations
            .FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken);

        if (efaConfig is null)
        {
            return Result.Failure<DeleteEfaConfigurationResponse>(
                EfaConfigurationErrors.NotFound(command.Id));
        }

        // Store values before deletion to include in the response
        var response = new DeleteEfaConfigurationResponse(
            efaConfig.Id,
            efaConfig.Year,
            efaConfig.EfaRate,
            dateTimeProvider.UtcNow,
            command.DeletedBy
        );

        // Remove the configuration
        context.EfaConfigurations.Remove(efaConfig);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(response);
    }
}
