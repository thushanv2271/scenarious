using System.Text.Json;
using System.Text.Json.Nodes;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.PDAlgorithmResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.PDAlgorithmResults.UpdateSelectedMethodology;

/// <summary>
/// Handler to update the selected methodology for a specific product category and segment
/// selectedMethodology is stored at segment level (outside summary)
/// </summary>
internal sealed class UpdateSelectedMethodologyCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    ILogger<UpdateSelectedMethodologyCommandHandler> logger)
    : ICommandHandler<UpdateSelectedMethodologyCommand, UpdateSelectedMethodologyResponse>
{
    private static readonly string[] ValidMethodologies = { "method1", "method2", "method3" };

    public async Task<Result<UpdateSelectedMethodologyResponse>> Handle(
        UpdateSelectedMethodologyCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Updating selected methodology for PD Result {Id}, Category: {Category}, Segment: {Segment}",
            command.Id, command.ProductCategory, command.Segment);

        // Validate methodology
        if (!ValidMethodologies.Contains(command.SelectedMethodology, StringComparer.OrdinalIgnoreCase))
        {
            return Result.Failure<UpdateSelectedMethodologyResponse>(
                PDAlgorithmResultErrors.InvalidMethodology(command.SelectedMethodology));
        }

        // Retrieve the PD Algorithm Result
        PDAlgorithmResult? result = await context.PDAlgorithmResults
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);

        if (result is null)
        {
            logger.LogWarning("PD Algorithm Result not found: {Id}", command.Id);
            return Result.Failure<UpdateSelectedMethodologyResponse>(
                PDAlgorithmResultErrors.NotFound(command.Id));
        }

        // Parse JSONB data
        var rootNode = JsonNode.Parse(result.PdAlgorithmResultData);
        if (rootNode is null)
        {
            logger.LogError("Failed to parse JSONB data for PD Result: {Id}", command.Id);
            return Result.Failure<UpdateSelectedMethodologyResponse>(
                PDAlgorithmResultErrors.UpdateFailed("Failed to parse stored JSON data"));
        }

        // Find and update the specific segment's selectedMethodology
        Result<bool> updateResult = UpdateSelectedMethodologyInJson(
            rootNode,
            command.ProductCategory,
            command.Segment,
            command.SelectedMethodology);

        if (updateResult.IsFailure)
        {
            return Result.Failure<UpdateSelectedMethodologyResponse>(updateResult.Error);
        }

        // Serialize updated JSON back to string
        string updatedJson = rootNode.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = false
        });

        // Update entity
        result.UpdateData(updatedJson, userContext.UserId);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Successfully updated selected methodology for PD Result {Id}",
            command.Id);

        return Result.Success(new UpdateSelectedMethodologyResponse
        {
            Id = result.Id,
            ProductCategory = command.ProductCategory,
            Segment = command.Segment,
            SelectedMethodology = command.SelectedMethodology,
            UpdatedAt = result.UpdatedAt ?? DateTime.UtcNow,
            UpdatedBy = result.UpdatedBy ?? userContext.UserId
        });
    }

    private Result<bool> UpdateSelectedMethodologyInJson(
        JsonNode rootNode,
        string productCategory,
        string segment,
        string selectedMethodology)
    {
        JsonArray? productCategories = rootNode["productCategories"]?.AsArray();
        if (productCategories is null)
        {
            return Result.Failure<bool>(
                PDAlgorithmResultErrors.UpdateFailed("Invalid JSON structure: productCategories not found"));
        }

        // Find the matching product category
        JsonNode? targetCategory = null;
        foreach (JsonNode? category in productCategories)
        {
            string? categoryName = category?["productCategory"]?.GetValue<string>();
            if (string.Equals(categoryName, productCategory, StringComparison.OrdinalIgnoreCase))
            {
                targetCategory = category;
                break;
            }
        }

        if (targetCategory is null)
        {
            return Result.Failure<bool>(
                PDAlgorithmResultErrors.InvalidProductCategory(productCategory));
        }

        // Find the matching segment
        JsonArray? segments = targetCategory["segments"]?.AsArray();
        if (segments is null)
        {
            return Result.Failure<bool>(
                PDAlgorithmResultErrors.UpdateFailed("Invalid JSON structure: segments not found"));
        }

        JsonObject? targetSegment = null;
        foreach (JsonNode? seg in segments)
        {
            string? segmentName = seg?["segment"]?.GetValue<string>();
            if (string.Equals(segmentName, segment, StringComparison.OrdinalIgnoreCase))
            {
                targetSegment = seg?.AsObject();
                break;
            }
        }

        if (targetSegment is null)
        {
            return Result.Failure<bool>(
                PDAlgorithmResultErrors.InvalidSegment(segment));
        }

        // UPDATE: selectedMethodology now goes at SEGMENT level (not inside summary)
        targetSegment["selectedMethodology"] = selectedMethodology;

        logger.LogDebug(
            "Updated selectedMethodology to '{Methodology}' at segment level for Category: {Category}, Segment: {Segment}",
            selectedMethodology, productCategory, segment);

        return Result.Success(true);
    }
}
