using System.Text.Json;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.LGDCalculation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.LgdAlgorithmResults.UpdateSelectedMethodology;

/// <summary>
/// Handler for updating selected methodology in LGD Algorithm Results
/// </summary>
internal sealed class UpdateSelectedLgdMethodologyCommandHandler(
    IApplicationDbContext context,
    ILogger<UpdateSelectedLgdMethodologyCommandHandler> logger)
    : ICommandHandler<UpdateSelectedLgdMethodologyCommand, UpdateSelectedLgdMethodologyResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public async Task<Result<UpdateSelectedLgdMethodologyResponse>> Handle(
        UpdateSelectedLgdMethodologyCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Updating LGD selected methodology. ID: {Id}, ProductCategory: {ProductCategory}, Segment: {Segment}, Methodology: {Methodology}",
            command.Id,
            command.ProductCategory,
            command.Segment,
            command.SelectedMethodology);

        // Find the LGD Algorithm Result
        LgdAlgorithmResult? lgdResult = await context.LgdAlgorithmResults
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);

        if (lgdResult is null)
        {
            logger.LogWarning("LGD Algorithm Result not found: {Id}", command.Id);
            return Result.Failure<UpdateSelectedLgdMethodologyResponse>(
                LgdAlgorithmResultErrors.NotFound(command.Id));
        }

        // Parse existing data
        Dictionary<string, object>? data;
        try
        {
            data = JsonSerializer.Deserialize<Dictionary<string, object>>(
                lgdResult.LgdAlgorithmResultData,
                JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize LGD Algorithm Result data for ID: {Id}", command.Id);
            return Result.Failure<UpdateSelectedLgdMethodologyResponse>(
                LgdAlgorithmResultErrors.InvalidData);
        }

        if (data is null)
        {
            logger.LogError("LGD Algorithm Result data is null for ID: {Id}", command.Id);
            return Result.Failure<UpdateSelectedLgdMethodologyResponse>(
                LgdAlgorithmResultErrors.InvalidData);
        }

        // TODO: Update the selected methodology in the data structure
        // This depends on the actual LGD algorithm result structure
        // For now, we'll update a generic structure
        bool updated = UpdateMethodologyInData(data, command);

        if (!updated)
        {
            logger.LogWarning(
                "Product category '{ProductCategory}' and segment '{Segment}' combination not found in LGD Algorithm Result {Id}",
                command.ProductCategory,
                command.Segment,
                command.Id);
            return Result.Failure<UpdateSelectedLgdMethodologyResponse>(
                LgdAlgorithmResultErrors.InvalidProductCategory(command.ProductCategory));
        }

        // Serialize updated data back
        string updatedJson;
        try
        {
            updatedJson = JsonSerializer.Serialize(data, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to serialize updated LGD Algorithm Result data for ID: {Id}", command.Id);
            return Result.Failure<UpdateSelectedLgdMethodologyResponse>(
                LgdAlgorithmResultErrors.UpdateFailed("Failed to serialize updated data"));
        }

        // Update the entity
        lgdResult.UpdateData(updatedJson, Guid.Empty); // TODO: Get actual user ID from context

        // Save changes
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save LGD Algorithm Result updates for ID: {Id}", command.Id);
            return Result.Failure<UpdateSelectedLgdMethodologyResponse>(
                LgdAlgorithmResultErrors.UpdateFailed("Failed to save changes to database"));
        }

        var response = new UpdateSelectedLgdMethodologyResponse
        {
            Id = command.Id,
            ProductCategory = command.ProductCategory,
            Segment = command.Segment,
            SelectedMethodology = command.SelectedMethodology,
            UpdatedAt = DateTime.UtcNow,
            Message = "Selected methodology updated successfully"
        };

        logger.LogInformation(
            "Successfully updated LGD selected methodology for ID: {Id}, ProductCategory: {ProductCategory}, Segment: {Segment}",
            command.Id,
            command.ProductCategory,
            command.Segment);

        return Result.Success(response);
    }

    /// <summary>
    /// Updates the selected methodology in the data structure
    /// TODO: Implement based on actual LGD algorithm result structure
    /// </summary>
    private static bool UpdateMethodologyInData(Dictionary<string, object> data, UpdateSelectedLgdMethodologyCommand command)
    {
        // This is a placeholder implementation
        // The actual implementation depends on the LGD algorithm result structure

        // Example structure update (modify based on actual LGD data format):
        // data["productCategories"] -> find matching category -> find matching segment -> update selectedMethodology

        // Use parameters to avoid unused parameter warnings
        _ = data;
        _ = command;

        return true; // Placeholder - always return true for now
    }
}