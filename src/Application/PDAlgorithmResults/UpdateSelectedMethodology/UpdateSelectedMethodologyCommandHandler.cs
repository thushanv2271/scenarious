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
/// Validates methodology against available methods in the database JSON
/// </summary>
internal sealed class UpdateSelectedMethodologyCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    ILogger<UpdateSelectedMethodologyCommandHandler> logger)
    : ICommandHandler<UpdateSelectedMethodologyCommand, UpdateSelectedMethodologyResponse>
{
    public async Task<Result<UpdateSelectedMethodologyResponse>> Handle(
        UpdateSelectedMethodologyCommand command,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Updating selected methodology for PD Result {Id}, Category: {Category}, Segment: {Segment}",
            command.Id, command.ProductCategory, command.Segment);

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

        // Find and validate the specific segment's available methodologies
        Result<(JsonObject targetSegment, List<string> availableMethodologies)> findResult =
            FindSegmentAndAvailableMethodologies(
                rootNode,
                command.ProductCategory,
                command.Segment);

        if (findResult.IsFailure)
        {
            return Result.Failure<UpdateSelectedMethodologyResponse>(findResult.Error);
        }

        (JsonObject targetSegment, List<string> availableMethodologies) = findResult.Value;

        // Validate that the selected methodology exists in available methodologies
        if (!availableMethodologies.Contains(command.SelectedMethodology, StringComparer.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Invalid methodology '{Methodology}' for Category: {Category}, Segment: {Segment}. Available: [{Available}]",
                command.SelectedMethodology, command.ProductCategory, command.Segment,
                string.Join(", ", availableMethodologies));

            return Result.Failure<UpdateSelectedMethodologyResponse>(
                PDAlgorithmResultErrors.InvalidMethodology(
                    command.SelectedMethodology,
                    availableMethodologies));
        }

        // Update selectedMethodology at SEGMENT level
        targetSegment["selectedMethodology"] = command.SelectedMethodology;

        logger.LogDebug(
            "Updated selectedMethodology to '{Methodology}' at segment level for Category: {Category}, Segment: {Segment}",
            command.SelectedMethodology, command.ProductCategory, command.Segment);

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

    /// <summary>
    /// Finds the target segment and extracts available methodologies from its summary
    /// </summary>
    private Result<(JsonObject targetSegment, List<string> availableMethodologies)> FindSegmentAndAvailableMethodologies(
        JsonNode rootNode,
        string productCategory,
        string segment)
    {
        JsonArray? productCategories = rootNode["productCategories"]?.AsArray();
        if (productCategories is null)
        {
            return Result.Failure<(JsonObject, List<string>)>(
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
            return Result.Failure<(JsonObject, List<string>)>(
                PDAlgorithmResultErrors.InvalidProductCategory(productCategory));
        }

        // Find the matching segment
        JsonArray? segments = targetCategory["segments"]?.AsArray();
        if (segments is null)
        {
            return Result.Failure<(JsonObject, List<string>)>(
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
            return Result.Failure<(JsonObject, List<string>)>(
                PDAlgorithmResultErrors.InvalidSegment(segment));
        }

        // Extract available methodologies from the summary
        List<string> availableMethodologies = ExtractAvailableMethodologies(targetSegment);

        if (availableMethodologies.Count == 0)
        {
            logger.LogWarning(
                "No methodologies found in summary for Category: {Category}, Segment: {Segment}",
                productCategory, segment);

            return Result.Failure<(JsonObject, List<string>)>(
                PDAlgorithmResultErrors.UpdateFailed(
                    $"No methodologies available for product category '{productCategory}' and segment '{segment}'"));
        }

        logger.LogDebug(
            "Found {Count} available methodologies for Category: {Category}, Segment: {Segment}: [{Methodologies}]",
            availableMethodologies.Count, productCategory, segment, string.Join(", ", availableMethodologies));

        return Result.Success((targetSegment, availableMethodologies));
    }

    /// <summary>
    /// Extracts available methodology names from the segment's summary
    /// </summary>
    private static List<string> ExtractAvailableMethodologies(JsonObject segmentNode)
    {
        List<string> methodologies = new();

        JsonObject? summary = segmentNode["summary"]?.AsObject();
        if (summary is null)
        {
            return methodologies;
        }

        // Check for each potential methodology in the summary
        foreach (KeyValuePair<string, JsonNode?> property in summary)
        {
            // Verify that the property has actual method data (not null)
            if (property.Value is JsonObject)
            {
                methodologies.Add(property.Key);
            }
        }

        return methodologies;
    }
}
