using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Files;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Files.DownloadFile;

internal sealed class DownloadFileQueryHandler(
    IApplicationDbContext dbContext
) : IQueryHandler<DownloadFileQuery, DownloadFileResult>
{
    public async Task<Result<DownloadFileResult>> Handle(DownloadFileQuery query, CancellationToken cancellationToken)
    {
        UploadedFile? entity = await dbContext.UploadedFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);

        if (entity is null)
        {
            return Result.Failure<DownloadFileResult>(
                Error.NotFound("UploadedFile.NotFound", $"Uploaded file '{query.Id}' was not found."));
        }

        // Verify physical file exists
        if (!File.Exists(entity.PhysicalPath))
        {
            return Result.Failure<DownloadFileResult>(
                Error.NotFound("File.PhysicalFileNotFound", $"Physical file not found at '{entity.PhysicalPath}'."));
        }

        var result = new DownloadFileResult(
            PhysicalPath: entity.PhysicalPath,
            OriginalFileName: entity.OriginalFileName,
            ContentType: entity.ContentType,
            Size: entity.Size
        );

        return Result.Success(result);
    }
}
