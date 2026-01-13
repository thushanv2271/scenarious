namespace Application.PDAlgorithmResults.GetSelectedMethodologies;

/// <summary>
/// Response containing all selected methodologies with pagination
/// </summary>
public sealed record GetSelectedMethodologiesResponse(
    List<PDAlgorithmResultMethodologyDto> Results,
    PaginationMetadata Pagination
);

/// <summary>
/// PD Algorithm Result containing methodology information
/// </summary>
public sealed record PDAlgorithmResultMethodologyDto(
    Guid Id,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<ProductCategoryMethodologyDto> ProductCategories
);

/// <summary>
/// Product category containing segments with selected methodologies
/// </summary>
public sealed record ProductCategoryMethodologyDto(
    string ProductCategory,
    List<SegmentMethodologyDto> Segments
);

/// <summary>
/// Segment with selected methodology and its data
/// </summary>
public sealed record SegmentMethodologyDto(
    string Segment,
    string SelectedMethodology,
    MethodologyDataDto? SelectedMethodologyData
);

/// <summary>
/// Complete methodology data structure containing all possible tables
/// </summary>
public sealed record MethodologyDataDto(
    PdTableDto? MarginalPdsAfterEfa,
    PdTableDto? ExtrapolatedCumulativePdsAfterEfa,
    PdTableDto? ExtrapolatedCumulativePdsBeforeEfa,
    PdTableDto? SurvivalRates,
    PdTableDto? MarginalPds,
    PdTableDto? EfaAdjustedPds
);

/// <summary>
/// PD Table with full structure
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
