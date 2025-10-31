using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Files;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Files.GetUploadedFile;

internal sealed class GetUploadedFileQueryHandler(
    IApplicationDbContext dbContext
) : IQueryHandler<GetUploadedFileQuery, GetUploadedFileResponse>
{
    public async Task<Result<GetUploadedFileResponse>> Handle(GetUploadedFileQuery query, CancellationToken cancellationToken)
    {
        UploadedFile? entity = await dbContext.UploadedFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, cancellationToken);

        if (entity is null)
        {
            return Result.Failure<GetUploadedFileResponse>(
                Error.NotFound("UploadedFile.NotFound", $"Uploaded file '{query.Id}' was not found."));
        }

        var response = new GetUploadedFileResponse(
            Id: entity.Id,
            OriginalFileName: entity.OriginalFileName,
            StoredFileName: entity.StoredFileName,
            ContentType: entity.ContentType,
            Size: entity.Size,
            Url: new Uri(entity.PublicUrl),
            UploadedBy: entity.UploadedBy,
            UploadedAt: entity.UploadedAt
        );

        return Result.Success(response);
    }
}
