
namespace Application.PDAlgorithmResults.GetSelectedMethodologies;

/// <summary>
/// Paginated response containing all selected methodologies
/// </summary>
public sealed record GetSelectedMethodologiesResponse(
    List<PDAlgorithmResultMethodologyDto> Results,
    PaginationMetadata Pagination
);

/// <summary>
/// Represents a single PD Algorithm Result with its selected methodologies
/// </summary>
public sealed record PDAlgorithmResultMethodologyDto(
    Guid Id,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<ProductCategoryMethodologyDto> ProductCategories
);

/// <summary>
/// Product category with its segments and their selected methodologies
/// </summary>
public sealed record ProductCategoryMethodologyDto(
    string ProductCategory,
    List<SegmentMethodologyDto> Segments
);

/// <summary>
/// Segment with its selected methodology and full data
/// </summary>
public sealed record SegmentMethodologyDto(
    string Segment,
    string? SelectedMethodology,
    MethodologyDataDto? SelectedMethodologyData
);

/// <summary>
/// Full methodology data containing all PD tables
/// </summary>
public sealed record MethodologyDataDto(
    PdTableDto? MarginalPdsAfterEfa,
    PdTableDto? ExtrapolatedCumulativePdsAfterEfa,
    PdTableDto? ExtrapolatedCumulativePdsBeforeEfa
);

/// <summary>
/// PD Table with title, headers, and rows
/// </summary>
public sealed record PdTableDto(
    string Title,
    List<string> ColumnHeaders,
    List<PdTableRowDto> Rows
);

/// <summary>
/// Single row in a PD table
/// </summary>
public sealed record PdTableRowDto(
    string AgeBucket,
    Dictionary<string, decimal?> PdValuesByYear
);

/// <summary>
/// Pagination metadata
/// </summary>
public sealed record PaginationMetadata(
    int CurrentPage,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage
);
