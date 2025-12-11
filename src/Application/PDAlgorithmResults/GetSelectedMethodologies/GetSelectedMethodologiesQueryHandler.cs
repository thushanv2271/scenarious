using System.Text.Json;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.PDAlgorithmResults;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.PDAlgorithmResults.GetSelectedMethodologies;

/// <summary>
/// Handles GetSelectedMethodologiesQuery - retrieves selected methodologies
/// with SEGMENT-BASED pagination (not record-based)
/// </summary>
internal sealed class GetSelectedMethodologiesQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<GetSelectedMethodologiesQuery, GetSelectedMethodologiesResponse>
{
    public async Task<Result<GetSelectedMethodologiesResponse>> Handle(
        GetSelectedMethodologiesQuery query,
        CancellationToken cancellationToken)
    {
        // Step 1: Get the latest PDAlgorithmResult
        PDAlgorithmResult? pdResult = await context.PDAlgorithmResults
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (pdResult == null || string.IsNullOrWhiteSpace(pdResult.PdAlgorithmResultData))
        {
            return CreateEmptyResponse(query);
        }

        // Step 2: Extract ALL segments with selectedMethodology
        List<FlatSegmentDto> allSegments = ExtractAllSegments(
            pdResult,
            query.ProductCategory,
            query.Segment,
            query.Search);

        if (allSegments.Count == 0)
        {
            return CreateEmptyResponse(query);
        }

        // Step 3: Apply SEGMENT-BASED pagination
        int totalSegments = allSegments.Count;
        int totalPages = (int)Math.Ceiling(totalSegments / (double)query.PageSize);
        int skip = (query.PageNumber - 1) * query.PageSize;

        var paginatedSegments = allSegments
            .Skip(skip)
            .Take(query.PageSize)
            .ToList();

        // Step 4: Group paginated segments back by ProductCategory
        List<ProductCategoryMethodologyDto> groupedCategories = GroupSegmentsByCategory(paginatedSegments);

        var result = new PDAlgorithmResultMethodologyDto(
            Id: pdResult.Id,
            CreatedAt: pdResult.CreatedAt,
            UpdatedAt: pdResult.UpdatedAt,
            ProductCategories: groupedCategories
        );

        var pagination = new PaginationMetadata(
            CurrentPage: query.PageNumber,
            PageSize: query.PageSize,
            TotalCount: totalSegments,  // Total SEGMENTS, not records
            TotalPages: totalPages,
            HasPreviousPage: query.PageNumber > 1,
            HasNextPage: query.PageNumber < totalPages
        );

        return new GetSelectedMethodologiesResponse(
            Results: new List<PDAlgorithmResultMethodologyDto> { result },
            Pagination: pagination
        );
    }

    private static GetSelectedMethodologiesResponse CreateEmptyResponse(GetSelectedMethodologiesQuery query)
    {
        return new GetSelectedMethodologiesResponse(
            new List<PDAlgorithmResultMethodologyDto>(),
            new PaginationMetadata(
                CurrentPage: query.PageNumber,
                PageSize: query.PageSize,
                TotalCount: 0,
                TotalPages: 0,
                HasPreviousPage: false,
                HasNextPage: false
            )
        );
    }

    /// <summary>
    /// Extracts all segments into a flat list for pagination
    /// </summary>
    private static List<FlatSegmentDto> ExtractAllSegments(
        PDAlgorithmResult pdResult,
        string? filterProductCategory,
        string? filterSegment,
        string? search)
    {
        var allSegments = new List<FlatSegmentDto>();

        try
        {
            using var document = JsonDocument.Parse(pdResult.PdAlgorithmResultData);
            JsonElement root = document.RootElement;

            if (!root.TryGetProperty("productCategories", out JsonElement productCategoriesElement))
            {
                return allSegments;
            }

            foreach (JsonElement categoryElement in productCategoriesElement.EnumerateArray())
            {
                string? categoryName = categoryElement.TryGetProperty("productCategory", out JsonElement catNameEl)
                    ? catNameEl.GetString()
                    : null;

                // Apply product category filter
                if (!string.IsNullOrWhiteSpace(filterProductCategory) &&
                    !string.Equals(categoryName, filterProductCategory, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!categoryElement.TryGetProperty("segments", out JsonElement segmentsElement))
                {
                    continue;
                }

                foreach (JsonElement segmentElement in segmentsElement.EnumerateArray())
                {
                    FlatSegmentDto? flatSegment = ParseSegmentToFlat(
                        segmentElement,
                        categoryName ?? "Unknown",
                        filterSegment,
                        search);

                    if (flatSegment != null)
                    {
                        allSegments.Add(flatSegment);
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Return empty list on parse error
        }

        return allSegments;
    }

    /// <summary>
    /// Parses a segment into a flat DTO (includes productCategory for grouping later)
    /// </summary>
    private static FlatSegmentDto? ParseSegmentToFlat(
        JsonElement segmentElement,
        string productCategory,
        string? filterSegment,
        string? search)
    {
        string? segmentName = segmentElement.TryGetProperty("segment", out JsonElement segNameEl)
            ? segNameEl.GetString()
            : null;

        // Apply segment filter
        if (!string.IsNullOrWhiteSpace(filterSegment) &&
            !string.Equals(segmentName, filterSegment, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Get selectedMethodology
        string? selectedMethodology = segmentElement.TryGetProperty("selectedMethodology", out JsonElement selMethodEl)
            ? selMethodEl.GetString()
            : null;

        // Only return segments WITH selectedMethodology
        if (string.IsNullOrWhiteSpace(selectedMethodology))
        {
            return null;
        }

        // Get methodology data
        MethodologyDataDto? methodologyData = null;
        if (segmentElement.TryGetProperty("summary", out JsonElement summaryElement))
        {
            methodologyData = ExtractMethodologyData(summaryElement, selectedMethodology);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search) &&
            !MatchesSearch(search, productCategory, segmentName, selectedMethodology, methodologyData))
        {
            return null;
        }

        return new FlatSegmentDto(
            ProductCategory: productCategory,
            Segment: segmentName ?? "Unknown",
            SelectedMethodology: selectedMethodology,
            SelectedMethodologyData: methodologyData
        );
    }

    /// <summary>
    /// Groups flat segments back into ProductCategory structure
    /// </summary>
    private static List<ProductCategoryMethodologyDto> GroupSegmentsByCategory(List<FlatSegmentDto> segments)
    {
        return segments
            .GroupBy(s => s.ProductCategory)
            .Select(g => new ProductCategoryMethodologyDto(
                ProductCategory: g.Key,
                Segments: g.Select(s => new SegmentMethodologyDto(
                    Segment: s.Segment,
                    SelectedMethodology: s.SelectedMethodology,
                    SelectedMethodologyData: s.SelectedMethodologyData
                )).ToList()
            ))
            .ToList();
    }

    #region JSON Extraction Methods

    private static MethodologyDataDto? ExtractMethodologyData(
        JsonElement summaryElement,
        string methodologyName)
    {
        if (!summaryElement.TryGetProperty(methodologyName, out JsonElement methodElement))
        {
            return null;
        }

        return new MethodologyDataDto(
            MarginalPdsAfterEfa: ExtractPdTable(methodElement, "marginalPdsAfterEfa"),
            ExtrapolatedCumulativePdsAfterEfa: ExtractPdTable(methodElement, "extrapolatedCumulativePdsAfterEfa"),
            ExtrapolatedCumulativePdsBeforeEfa: ExtractPdTable(methodElement, "extrapolatedCumulativePdsBeforeEfa")
        );
    }

    /// <summary>
    /// Extracts a single PD table with title, headers, and rows
    /// </summary>
    private static PdTableDto? ExtractPdTable(JsonElement methodElement, string tableName)
    {
        if (!methodElement.TryGetProperty(tableName, out JsonElement tableElement))
        {
            return null;
        }

        string title = GetStringProperty(tableElement, "title");
        List<string> columnHeaders = ExtractColumnHeaders(tableElement);
        List<PdTableRowDto> rows = ExtractTableRows(tableElement);

        return new PdTableDto(
            Title: title,
            ColumnHeaders: columnHeaders,
            Rows: rows
        );
    }

    /// <summary>
    /// Gets a string property from a JSON element
    /// </summary>
    private static string GetStringProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement propEl) &&
               propEl.ValueKind == JsonValueKind.String
            ? propEl.GetString() ?? string.Empty
            : string.Empty;
    }

    /// <summary>
    /// Extracts column headers from a table element
    /// </summary>
    private static List<string> ExtractColumnHeaders(JsonElement tableElement)
    {
        var columnHeaders = new List<string>();

        if (!tableElement.TryGetProperty("columnHeaders", out JsonElement headersEl) ||
            headersEl.ValueKind != JsonValueKind.Array)
        {
            return columnHeaders;
        }

        foreach (JsonElement header in headersEl.EnumerateArray())
        {
            if (header.ValueKind == JsonValueKind.String)
            {
                columnHeaders.Add(header.GetString() ?? string.Empty);
            }
        }

        return columnHeaders;
    }

    /// <summary>
    /// Extracts all rows from a table element
    /// </summary>
    private static List<PdTableRowDto> ExtractTableRows(JsonElement tableElement)
    {
        var rows = new List<PdTableRowDto>();

        if (!tableElement.TryGetProperty("rows", out JsonElement rowsEl) ||
            rowsEl.ValueKind != JsonValueKind.Array)
        {
            return rows;
        }

        foreach (JsonElement rowElement in rowsEl.EnumerateArray())
        {
            rows.Add(ExtractPdTableRow(rowElement));
        }

        return rows;
    }

    /// <summary>
    /// Extracts a single row from a PD table
    /// </summary>
    private static PdTableRowDto ExtractPdTableRow(JsonElement rowElement)
    {
        string ageBucket = GetStringProperty(rowElement, "ageBucket");
        Dictionary<string, decimal?> pdValuesByYear = ExtractPdValuesByYear(rowElement);

        return new PdTableRowDto(
            AgeBucket: ageBucket,
            PdValuesByYear: pdValuesByYear
        );
    }

    /// <summary>
    /// Extracts pdValuesByYear dictionary from a row element
    /// </summary>
    private static Dictionary<string, decimal?> ExtractPdValuesByYear(JsonElement rowElement)
    {
        var pdValuesByYear = new Dictionary<string, decimal?>();

        if (!rowElement.TryGetProperty("pdValuesByYear", out JsonElement pdValuesEl) ||
            pdValuesEl.ValueKind != JsonValueKind.Object)
        {
            return pdValuesByYear;
        }

        foreach (JsonProperty property in pdValuesEl.EnumerateObject())
        {
            pdValuesByYear[property.Name] = ParseDecimalValue(property.Value);
        }

        return pdValuesByYear;
    }

    /// <summary>
    /// Parses a decimal value from a JSON element
    /// </summary>
    private static decimal? ParseDecimalValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.GetDecimal(),
            JsonValueKind.Null => null,
            JsonValueKind.String when decimal.TryParse(element.GetString(), out decimal parsed) => parsed,
            _ => null
        };
    }

    #endregion

    #region Search Methods

    /// <summary>
    /// Checks if any field matches the search term (case-insensitive)
    /// Flattened logic to reduce nesting complexity (S1066)
    /// </summary>
    private static bool MatchesSearch(
        string search,
        string? productCategory,
        string? segment,
        string? selectedMethodology,
        MethodologyDataDto? methodologyData)
    {
        // Search in basic fields (combined with OR to reduce nesting)
        if (ContainsIgnoreCase(productCategory, search) ||
            ContainsIgnoreCase(segment, search) ||
            ContainsIgnoreCase(selectedMethodology, search))
        {
            return true;
        }

        // Search in methodology data
        return methodologyData != null && MatchesSearchInMethodologyData(methodologyData, search);
    }

    /// <summary>
    /// Searches within all methodology data tables
    /// </summary>
    private static bool MatchesSearchInMethodologyData(MethodologyDataDto methodologyData, string search)
    {
        return MatchesSearchInTable(methodologyData.MarginalPdsAfterEfa, search) ||
               MatchesSearchInTable(methodologyData.ExtrapolatedCumulativePdsAfterEfa, search) ||
               MatchesSearchInTable(methodologyData.ExtrapolatedCumulativePdsBeforeEfa, search);
    }

    /// <summary>
    /// Searches within a PD table (title, headers, and ageBuckets)
    /// Uses LINQ Any() to flatten loops
    /// </summary>
    private static bool MatchesSearchInTable(PdTableDto? table, string search)
    {
        if (table == null)
        {
            return false;
        }

        // Flattened search using OR conditions
        return ContainsIgnoreCase(table.Title, search) ||
               table.ColumnHeaders.Any(header => ContainsIgnoreCase(header, search)) ||
               table.Rows.Any(row => ContainsIgnoreCase(row.AgeBucket, search));
    }

    /// <summary>
    /// Case-insensitive contains check using StringComparison.OrdinalIgnoreCase
    /// Flattened to single return statement
    /// </summary>
    private static bool ContainsIgnoreCase(string? source, string search)
    {
        return !string.IsNullOrWhiteSpace(source) &&
               source.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}

/// <summary>
/// Internal flat segment DTO for pagination processing
/// </summary>
internal sealed record FlatSegmentDto(
    string ProductCategory,
    string Segment,
    string SelectedMethodology,
    MethodologyDataDto? SelectedMethodologyData
);
