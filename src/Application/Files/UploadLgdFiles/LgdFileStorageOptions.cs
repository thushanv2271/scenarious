namespace Application.Files.UploadLgdFiles;

/// <summary>
/// Configuration options for LGD file storage location and access URLs.
/// </summary>
public sealed class LgdFileStorageOptions
{
    /// <summary>
    /// Root path where LGD files are stored on the file system.
    /// Example: /mnt/data/appdata/saral-backend-dev/data
    /// </summary>
    public required string RootPath { get; init; }
    
    /// <summary>
    /// Public base URL for accessing uploaded LGD files via HTTP.
    /// Example: https://be.dev.saral.digital/static/lgd-uploads
    /// </summary>
    public string? PublicBaseUrl { get; init; }
    
    /// <summary>
    /// Request path for serving static LGD files.
    /// Example: /static/lgd-uploads
    /// </summary>
    public string? RequestPath { get; init; }
}
