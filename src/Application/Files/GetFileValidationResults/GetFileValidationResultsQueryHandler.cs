using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Files.GetFileValidationResults;

internal sealed class GetFileValidationResultsQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<GetFileValidationResultsQuery, PaginatedResult<FileValidationResultResponse>>
{
    public async Task<Result<PaginatedResult<FileValidationResultResponse>>> Handle(
        GetFileValidationResultsQuery request, 
        CancellationToken cancellationToken)
    {
        IQueryable<Domain.Files.FileValidationResult> validationResultsQuery = context.FileValidationResults
            .Where(x => x.Status == "Success" || x.Status == "Error") // Exclude Warning status
            .OrderBy(x => x.Id);

        // Apply status filtering
        if (!string.IsNullOrWhiteSpace(request.Status) && request.Status != "all")
        {
            validationResultsQuery = request.Status.ToUpperInvariant() switch
            {
                "VALID" => validationResultsQuery.Where(x => x.Status == "Success"),
                "INVALID" => validationResultsQuery.Where(x => x.Status == "Error"),
                _ => validationResultsQuery
            };
        }

        int totalCount = await validationResultsQuery.CountAsync(cancellationToken);

        List<FileValidationResultResponse> validationResults = await validationResultsQuery
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new FileValidationResultResponse(
                x.Id,
                x.Filename,
                x.TotalRows,
                x.TotalErrors,
                x.Status == "Success" ? "valid" : "invalid"))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<FileValidationResultResponse>(validationResults, totalCount);
    }
}
