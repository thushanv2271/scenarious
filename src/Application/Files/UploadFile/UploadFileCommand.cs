using Application.Abstractions.Messaging;

namespace Application.Files.UploadFile;

/// <summary>
/// Represents a command to upload a file, including metadata and content.
/// </summary>
/// <remarks>This command encapsulates the details required to upload a file, such as the uploader's identifier,
/// the file's name, its content type, and the binary content. It is intended to be used in scenarios where file uploads
/// are processed as part of a command-handling pipeline.</remarks>
/// <param name="UploadedBy">The unique identifier of the user or entity uploading the file.</param>
/// <param name="Files">The files being uploaded, including metadata and content.</param>
/// <param name="CollectiveImpairmentType">The type of collective impairment (PD, LGD, EAD, EFA).</param>
/// <param name="TimePeriod">The time period this file belongs to (e.g., "2025", "2025-Q3", "2025-12").</param>
public sealed record UploadFileCommand(
    Guid UploadedBy,
    IReadOnlyList<FileUploadData> Files,
    string CollectiveImpairmentType,
    string TimePeriod
) : ICommand<UploadFileResponse>;

/// <summary>
/// Represents the data for a single file upload.
/// </summary>
/// <param name="FileName">The name of the file being uploaded, including its extension.</param>
/// <param name="ContentType">The MIME type of the file, such as "application/pdf" or "image/png".</param>
/// <param name="Content">The binary content of the file as a byte array.</param>
public sealed record FileUploadData(
    string FileName,
    string ContentType,
    byte[] Content
);
