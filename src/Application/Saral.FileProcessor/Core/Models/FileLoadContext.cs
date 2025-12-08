namespace Saral.FileProcessor.Core.Models;

public sealed record FileLoadContext(
    string FilePath,
    string Extension,
    long SizeInBytes,
    string? EncodingName,
    Frame<int, string> Data
);

public sealed record FileLoadInfo
{
    public required string FileName { get; init; }
    public required FileLoadContext LoadContext { get; init; }
    public required int FileIndex { get; init; }
}