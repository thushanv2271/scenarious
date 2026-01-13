using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Files.GetLgdFileValidationResults;

/// <summary>
/// Query to get LGD file validation results with pagination.
/// </summary>
public sealed record GetLgdFileValidationResultsQuery(
    string Year,
    string FacilityStatus,
    string? Status = null,
    int PageNumber = 1,
    int PageSize = 10
) : IQuery<LgdFileValidationResultsResponse>;

/// <summary>
/// Response for LGD file validation results with pagination.
/// </summary>
public sealed record LgdFileValidationResultsResponse
{
    public List<LgdFileValidationResultDto> Data { get; init; } = [];
    public int Total { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
};

/// <summary>
/// Individual LGD file validation result.
/// </summary>
public sealed record LgdFileValidationResultDto
{
    public int Id { get; init; }
    public string Filename { get; init; } = string.Empty;
    public int TotalRows { get; init; }
    public int TotalErrors { get; init; }
    public LgdFileValidationStatus Status { get; init; }
    public string? PhysicalPath { get; init; }
    public string? UploadedFileId { get; init; }
};

/// <summary>
/// Validation status for LGD files.
/// </summary>
public enum LgdFileValidationStatus
{
    /// <summary>
    /// File has passed validation with no errors.
    /// </summary>
    Valid,

    /// <summary>
    /// File has validation errors.
    /// </summary>
    Invalid
}
