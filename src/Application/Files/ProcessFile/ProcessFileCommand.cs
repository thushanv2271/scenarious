using Application.Abstractions.Messaging;

namespace Application.Files.ProcessFile;

/// <summary>
/// Represents a command to process a file and generate reports.
/// </summary>
/// <param name="UploadedBy">The unique identifier of the user processing the file.</param>
/// <param name="FileName">The name of the file being processed.</param>
/// <param name="Content">The binary content of the file as a byte array.</param>
public sealed record ProcessFileCommand(
    Guid UploadedBy,
    string FileName,
    byte[] Content
) : ICommand<ProcessFileResponse>;