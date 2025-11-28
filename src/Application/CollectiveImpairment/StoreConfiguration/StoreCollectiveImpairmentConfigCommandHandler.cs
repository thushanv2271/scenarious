using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.CollectiveImpairment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.CollectiveImpairment.StoreConfiguration;

internal sealed class StoreCollectiveImpairmentConfigCommandHandler(
    ILogger<StoreCollectiveImpairmentConfigCommandHandler> logger,
    IApplicationDbContext dbContext)
    : ICommandHandler<StoreCollectiveImpairmentConfigCommand>
{
    public async Task<Result> Handle(StoreCollectiveImpairmentConfigCommand command, CancellationToken cancellationToken)
    {
        if (command is null)
        {
            logger.LogError("Command is null.");
            throw new ArgumentNullException(nameof(command));
        }

        CollectiveImpairmentConfig? existingConfig = await dbContext.CollectiveImpairmentConfigs
            .FirstOrDefaultAsync(c => c.Parameter == command.Parameter, cancellationToken);

        if (existingConfig is not null)
        {
            existingConfig.ConfigJson = command.ConfigJson.ToJsonString();
            existingConfig.UpdatedDate = DateTime.UtcNow;
            existingConfig.UpdatedBy = command.UserId;

            dbContext.CollectiveImpairmentConfigs.Update(existingConfig);

            logger.LogInformation("Existing collective impairment config for {Parameter} updated.", command.Parameter);
        }
        else
        {
            var entity = new CollectiveImpairmentConfig
            {
                Id = Guid.NewGuid(),
                Parameter = command.Parameter,
                ConfigJson = command.ConfigJson.ToJsonString(),
                CreatedDate = DateTime.UtcNow,
                CreatedBy = command.UserId
            };

            dbContext.CollectiveImpairmentConfigs.Add(entity);

            logger.LogInformation("New collective impairment config for {Parameter} created.", command.Parameter);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Collective impairment config saved successfully.");

        return Result.Success();
    }
}
