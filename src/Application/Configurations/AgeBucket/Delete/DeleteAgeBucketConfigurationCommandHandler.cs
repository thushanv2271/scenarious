using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Configurations.AgeBucket.Delete;

internal sealed class DeleteAgeBucketConfigurationCommandHandler(
    IApplicationDbContext context)
    : ICommandHandler<DeleteAgeBucketConfigurationCommand, bool>
{
    public async Task<Result<bool>> Handle(
        DeleteAgeBucketConfigurationCommand command,
        CancellationToken cancellationToken)
    {
        Domain.Configurations.AgeBucketConfiguration? configuration = await context.AgeBucketConfigurations
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);

        if (configuration is null)
        {
            return Result.Failure<bool>(
                new Error("AgeBucketConfiguration.NotFound", $"Age bucket configuration with ID '{command.Id}' was not found.", ErrorType.NotFound));
        }

        context.AgeBucketConfigurations.Remove(configuration);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}