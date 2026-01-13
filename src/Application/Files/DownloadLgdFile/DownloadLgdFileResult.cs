namespace Application.Files.DownloadLgdFile;

/// <summary>
/// Result containing file information for download.
/// </summary>
/// <param name="PhysicalPath">The physical path to the file on disk.</param>
/// <param name="OriginalFileName">The original name of the file.</param>
/// <param name="ContentType">The MIME content type of the file.</param>
/// <param name="Size">The size of the file in bytes.</param>
public sealed record DownloadLgdFileResult(
    string PhysicalPath,
    string OriginalFileName,
    string ContentType,
    long Size
);
