using Application.Abstractions.Messaging;
using Microsoft.Extensions.Logging;
using NJsonSchema;
using SharedKernel;
using System.Text.Json.Nodes;
using Domain.PDTempData;
using Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.PD.PDSetup_TempStore;

/// <summary>
/// Handles the PDSetupTempStoreCommand.
/// </summary>
internal sealed class PDSetupTempStoreCommandHandler(
    ILogger<PDSetupTempStoreCommandHandler> logger,
    IApplicationDbContext dbContext) // inject dbContext
    : ICommandHandler<PDSetupTempStoreCommand>
{
    public async Task<Result> Handle(PDSetupTempStoreCommand command, CancellationToken cancellationToken)
    {
        if (command is null)
        {
            logger.LogError("Command is null.");
            throw new ArgumentNullException(nameof(command));
        }

        JsonObject stepsJson = command.StepsJson;

        bool isValid = await ValidateStepsJsonAsync(stepsJson, cancellationToken);

        if (!isValid)
        {
            logger.LogWarning("Steps JSON validation failed against schema.");
            throw new InvalidOperationException("Steps JSON does not match required schema.");
        }

        // Ensure only one record in PDTempData
        PDTempData? existingEntity = await dbContext.PDTempDatas.FirstOrDefaultAsync(cancellationToken);

        if (existingEntity is not null)
        {
            // Update existing record
            existingEntity.PDSetupJson = stepsJson.ToJsonString();
            existingEntity.UpdatedDate = DateTime.UtcNow;
            existingEntity.UpdatedBy = command.userId;

            dbContext.PDTempDatas.Update(existingEntity);

            logger.LogInformation("Existing PDTempData record updated.");
        }
        else
        {
            // Create new record
            var entity = new PDTempData
            {
                Id = Guid.NewGuid(),
                PDSetupJson = stepsJson.ToJsonString(),
                CreatedDate = DateTime.UtcNow,
                CreatedBy = command.userId
            };

            dbContext.PDTempDatas.Add(entity);

            logger.LogInformation("New PDTempData record created.");
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("PD setup temp store command validated and saved successfully.");

        return Result.Success();
    }

    /// <summary>
    /// Validates the steps JSON structure and values using JSON Schema.
    /// </summary>
    private async Task<bool> ValidateStepsJsonAsync(JsonObject stepsJson, CancellationToken cancellationToken)
    {


        string schemaJson = await PDSetupStepsSchemaProvider.GetSchemaAsync("Application.Schemas.PDSetupSteps.schema.json", cancellationToken);
        JsonSchema schema = await JsonSchema.FromJsonAsync(schemaJson, cancellationToken);
        string json = stepsJson.ToJsonString();
        ICollection<NJsonSchema.Validation.ValidationError> errors = schema.Validate(json);

        if (errors.Count > 0)
        {
            foreach (NJsonSchema.Validation.ValidationError error in errors)
            {
                logger.LogWarning("Schema validation error: {Error}", error.ToString());
            }
            return false;
        }

        return true;
    }
}
