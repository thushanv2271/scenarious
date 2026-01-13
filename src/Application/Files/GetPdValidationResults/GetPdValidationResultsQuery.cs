using Application.Abstractions.Messaging;

namespace Application.Files.GetPdValidationResults;

/// <summary>
/// Query to get PD file validation results from JSON configuration
/// </summary>
public sealed record GetPdValidationResultsQuery(
    int PageNumber,
    int PageSize,
    string? TimePeriod,
    string? Status
) : IQuery<PaginatedValidationResultsResponse>;

/// <summary>
/// Paginated response for validation results
/// </summary>
public sealed record PaginatedValidationResultsResponse(
    List<PdFileValidationInfo> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages
);

/// <summary>
/// Information about a file's validation status
/// </summary>
public sealed record PdFileValidationInfo(
    string UploadId,
    string FileName,
    string TimePeriod,
    string Status,
    int TotalRows,
    int TotalErrors,
    bool IsValidated,
    DateTime UploadedAt,
    List<ValidationErrorInfo>? Errors
);

/// <summary>
/// Individual validation error information
/// </summary>
public sealed record ValidationErrorInfo(
    int Row,
    string Column,
    string Error
);
