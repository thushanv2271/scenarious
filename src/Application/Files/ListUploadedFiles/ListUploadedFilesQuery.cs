using Application.Abstractions.Messaging;

namespace Application.Files.ListUploadedFiles;

public sealed record ListUploadedFilesQuery() : IQuery<List<ListUploadedFilesResponse>>;
