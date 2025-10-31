using Application.Abstractions.Storage;

namespace Infrastructure.Storage;

public class FileStorageService : IStorageService
{
    private readonly string _basePath;

    public FileStorageService(string basePath)
    {
        _basePath = basePath;
    }

    public async Task<string> SaveAsync(byte[] fileBytes, string fileName, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }

        string filePath = Path.Combine(_basePath, fileName);
        await File.WriteAllBytesAsync(filePath, fileBytes, cancellationToken);

        // Return local path
        return filePath;
    }
}
