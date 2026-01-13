using System.Text.Json;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.LGDCalculation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.LgdAlgorithmResults.UpdateLgdAlgorithmResult;

/// <summary>
/// Handler to update the LGD Algorithm Result JSON data
/// </summary>
internal sealed class UpdateLgdAlgorithmResultCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    ILogger<UpdateLgdAlgorithmResultCommandHandler> logger)
    : ICommandHandler<UpdateLgdAlgorithmResultCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        UpdateLgdAlgorithmResultCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating LGD Algorithm Result data");

        // Validate JSON format
        try
        {
            JsonDocument.Parse(command.LgdAlgorithmResultData);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Invalid JSON format provided for LGD Algorithm Result update");
            return Result.Failure<Guid>(LgdAlgorithmResultErrors.InvalidJsonFormat);
        }

        // Get the latest result to update
        LgdAlgorithmResult? existingResult = await context.LgdAlgorithmResults
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingResult is null)
        {
            logger.LogWarning("No existing LGD Algorithm Result found to update");
            return Result.Failure<Guid>(LgdAlgorithmResultErrors.NoResultsFound);
        }

        // Update the existing result
        existingResult.UpdateData(command.LgdAlgorithmResultData, userContext.UserId);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Successfully updated LGD Algorithm Result with ID: {Id}", existingResult.Id);

        return Result.Success(existingResult.Id);
    }
}