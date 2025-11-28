using Application.Abstractions.Data;
using Domain.ProductCategories;
using Domain.Segments;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.ProductCategories;

public sealed class SearchProductCategoriesAndSegmentsHandler(IApplicationDbContext dbContext)
{
    public async Task<Result<SearchProductCategoriesAndSegmentsResponse>> Handle(
        SearchProductCategoriesAndSegmentsCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IQueryable<ProductCategory> query = dbContext.ProductCategories.AsQueryable();

            // Apply type filter if provided
            if (!string.IsNullOrWhiteSpace(command.Type) && Enum.TryParse<ProductCategoryType>(command.Type, true, out ProductCategoryType filterType))
            {
                query = query.Where(pc => pc.Type == filterType);
            }

            // Get product categories with their segments
            List<ProductCategory> productCategories = await query
                .OrderBy(pc => pc.Type)
                .ThenBy(pc => pc.Name)
                .ToListAsync(cancellationToken);

            List<ProductCategoryWithSegmentsResponse> result = [];

            foreach (ProductCategory category in productCategories)
            {
                // Get segments for this category
                List<Segment> segments = await dbContext.Segments
                    .Where(s => s.ProductCategoryId == category.Id)
                    .OrderBy(s => s.Name)
                    .ToListAsync(cancellationToken);

                var segmentResponses = segments.Select(s => new SegmentResponse
                {
                    Id = s.Id,
                    Name = s.Name,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt
                }).ToList();

                result.Add(new ProductCategoryWithSegmentsResponse
                {
                    Id = category.Id,
                    Type = category.Type.ToString(),
                    Name = category.Name,
                    CreatedAt = category.CreatedAt,
                    UpdatedAt = category.UpdatedAt,
                    Segments = segmentResponses
                });
            }

            return new SearchProductCategoriesAndSegmentsResponse
            {
                ProductCategories = result,
                TotalCount = result.Count,
                FilteredByType = command.Type
            };
        }
        catch (Exception ex)
        {
            return Result.Failure<SearchProductCategoriesAndSegmentsResponse>(
                new Error("SEARCH_ERROR", $"Error searching product categories: {ex.Message}", ErrorType.Failure));
        }
    }
}