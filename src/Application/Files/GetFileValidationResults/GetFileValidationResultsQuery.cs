using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Files.GetFileValidationResults;

public sealed record GetFileValidationResultsQuery(
    int PageNumber,
    int PageSize,
    string? Status = null
) : IQuery<PaginatedResult<FileValidationResultResponse>>;