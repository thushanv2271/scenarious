
using System.Text.Json;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.PDAlgorithmResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.PDAlgorithmResults.GetPDAlgorithmResult;

/// <summary>
/// Handler to retrieve the latest PD Algorithm Result
/// Supports optional productCategory and segment filtering
/// </summary>
internal sealed class GetPDAlgorithmResultQueryHandler(
    IApplicationDbContext context,
    ILogger<GetPDAlgorithmResultQueryHandler> logger)
    : IQueryHandler<GetPDAlgorithmResultQuery, PDAlgorithmResultResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<Result<PDAlgorithmResultResponse>> Handle(
        GetPDAlgorithmResultQuery query,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Retrieving latest PD Algorithm Result. ProductCategory: {ProductCategory}, Segment: {Segment}",
            query.ProductCategory ?? "All",
            query.Segment ?? "All");

        PDAlgorithmResult? result = await context.PDAlgorithmResults
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (result is null)
        {
            logger.LogWarning("No PD Algorithm Results found in database");
            return Result.Failure<PDAlgorithmResultResponse>(
                PDAlgorithmResultErrors.NoResultsFound);
        }

        PDAlgorithmData? data = JsonSerializer.Deserialize<PDAlgorithmData>(
            result.PdAlgorithmResultData,
            JsonOptions);

        if (data is null)
        {
            logger.LogError("Failed to deserialize PD Algorithm Result data for ID: {Id}", result.Id);
            return Result.Failure<PDAlgorithmResultResponse>(
                PDAlgorithmResultErrors.UpdateFailed("Failed to deserialize stored data"));
        }

        // Apply filters
        Result<PDAlgorithmData> filterResult = ApplyFilters(
            data,
            query.ProductCategory,
            query.Segment);

        if (filterResult.IsFailure)
        {
            return Result.Failure<PDAlgorithmResultResponse>(filterResult.Error);
        }

        var response = new PDAlgorithmResultResponse
        {
            Id = result.Id,
            Data = filterResult.Value,
            CreatedAt = result.CreatedAt,
            CreatedBy = result.CreatedBy,
            UpdatedAt = result.UpdatedAt,
            UpdatedBy = result.UpdatedBy
        };

        logger.LogInformation(
            "Successfully retrieved PD Algorithm Result with ID: {Id}",
            result.Id);

        return Result.Success(response);
    }

    private Result<PDAlgorithmData> ApplyFilters(
        PDAlgorithmData data,
        string? productCategory,
        string? segment)
    {
        var filteredCategories = data.ProductCategories.ToList();

        // Filter by productCategory if provided
        if (!string.IsNullOrWhiteSpace(productCategory))
        {
            filteredCategories = filteredCategories
                .Where(pc => string.Equals(
                    pc.ProductCategory,
                    productCategory,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (filteredCategories.Count == 0)
            {
                logger.LogWarning("Product category '{ProductCategory}' not found", productCategory);
                return Result.Failure<PDAlgorithmData>(
                    PDAlgorithmResultErrors.InvalidProductCategory(productCategory));
            }
        }

        // Filter by segment if provided
        if (!string.IsNullOrWhiteSpace(segment))
        {
            filteredCategories = filteredCategories
                .Select(pc => new ProductCategoryData
                {
                    ProductCategory = pc.ProductCategory,
                    Segments = pc.Segments
                        .Where(s => string.Equals(
                            s.Segment,
                            segment,
                            StringComparison.OrdinalIgnoreCase))
                        .ToList()
                })
                .Where(pc => pc.Segments.Count > 0)
                .ToList();

            if (filteredCategories.Count == 0)
            {
                logger.LogWarning("Segment '{Segment}' not found", segment);
                return Result.Failure<PDAlgorithmData>(
                    PDAlgorithmResultErrors.InvalidSegment(segment));
            }
        }

        return Result.Success(new PDAlgorithmData
        {
            ProductCategories = filteredCategories
        });
    }
}
