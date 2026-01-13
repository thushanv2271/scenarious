using Application.Abstractions.Messaging;

namespace Application.Files.DownloadFile;

public sealed record DownloadFileQuery(Guid Id) : IQuery<DownloadFileResult>;
