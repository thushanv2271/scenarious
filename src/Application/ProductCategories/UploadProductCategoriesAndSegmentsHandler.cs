using Application.Abstractions.Data;
using Domain.ProductCategories;
using Domain.Segments;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.ProductCategories;

public sealed class UploadProductCategoriesAndSegmentsHandler(
    ICsvParsingService csvParsingService,
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider)
{
    public async Task<Result<CsvUploadResult>> Handle(Stream csvStream, CancellationToken cancellationToken = default)
    {
        try
        {
            // Parse CSV
            List<CsvRowData> csvData = await csvParsingService.ParseCsvAsync(csvStream);

            if (!csvData.Any())
            {
                return Result.Failure<CsvUploadResult>(new Error("CSV_EMPTY", "CSV file is empty or has no valid data", ErrorType.Validation));
            }

            List<string> errors = [];
            int categoriesCreated = 0;
            int segmentsCreated = 0;
            DateTime now = dateTimeProvider.UtcNow;

            // Get existing categories to avoid duplicates
            List<ProductCategory> existingCategories = await dbContext.ProductCategories.ToListAsync(cancellationToken);
            var categoryLookup = existingCategories.ToDictionary(c => $"{c.Type}_{c.Name}", c => c);

            foreach (CsvRowData row in csvData)
            {
                // Validate row data
                if (string.IsNullOrWhiteSpace(row.Type) ||
                    string.IsNullOrWhiteSpace(row.ProductCategory) ||
                    string.IsNullOrWhiteSpace(row.Segment))
                {
                    errors.Add($"Invalid row data: Type='{row.Type}', ProductCategory='{row.ProductCategory}', Segment='{row.Segment}'");
                    continue;
                }

                // Find or create product category
                string categoryKey = $"{row.Type}_{row.ProductCategory}";
                ProductCategory category;

                if (categoryLookup.TryGetValue(categoryKey, out ProductCategory? existingCategory))
                {
                    category = existingCategory;
                }
                else
                {
                    category = new ProductCategory
                    {
                        Id = Guid.NewGuid(),
                        Type = row.Type,
                        Name = row.ProductCategory,
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    dbContext.ProductCategories.Add(category);
                    categoryLookup[categoryKey] = category;
                    categoriesCreated++;
                }

                // Check if segment already exists for this category
                bool segmentExists = await dbContext.Segments
                    .AnyAsync(s => s.ProductCategoryId == category.Id && s.Name == row.Segment, cancellationToken);

                if (!segmentExists)
                {
                    Segment segment = new()
                    {
                        Id = Guid.NewGuid(),
                        ProductCategoryId = category.Id,
                        Name = row.Segment,
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    dbContext.Segments.Add(segment);
                    segmentsCreated++;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return new CsvUploadResult
            {
                ProcessedRecords = csvData.Count,
                CategoriesCreated = categoriesCreated,
                SegmentsCreated = segmentsCreated,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            return Result.Failure<CsvUploadResult>(new Error("CSV_PROCESSING_ERROR", $"Error processing CSV: {ex.Message}", ErrorType.Failure));
        }
    }
}