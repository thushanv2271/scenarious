using Application.Abstractions.Messaging;

namespace Application.Files.GetUploadedFile;

public sealed record GetUploadedFileQuery(Guid Id) : IQuery<GetUploadedFileResponse>;
