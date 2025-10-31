using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Files.ListUploadedFiles;

internal sealed class ListUploadedFilesQueryHandler(
    IApplicationDbContext dbContext
) : IQueryHandler<ListUploadedFilesQuery, List<ListUploadedFilesResponse>>
{
    public async Task<Result<List<ListUploadedFilesResponse>>> Handle(ListUploadedFilesQuery query, CancellationToken cancellationToken)
    {
        List<ListUploadedFilesResponse> items = await dbContext.UploadedFiles
            .AsNoTracking()
            .OrderByDescending(x => x.UploadedAt)
            .Select(x => new ListUploadedFilesResponse(
                x.Id,
                x.OriginalFileName,
                x.StoredFileName,
                x.ContentType,
                x.Size,
                new Uri(x.PublicUrl),
                x.UploadedBy,
                x.UploadedAt
            ))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
