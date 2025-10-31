using Application.Abstractions.Storage;
using Azure.Storage.Blobs;

namespace Infrastructure.Storage;

public class BlobStorageService : IStorageService
{
    private readonly BlobContainerClient _containerClient;

    public BlobStorageService(BlobContainerClient containerClient)
    {
        _containerClient = containerClient;
    }

    public async Task<string> SaveAsync(byte[] fileBytes, string fileName, CancellationToken cancellationToken)
    {
        BlobClient blobClient = _containerClient.GetBlobClient(fileName);
        using var stream = new MemoryStream(fileBytes);
        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken);

        // Return Blob URL
        return blobClient.Uri.ToString();
    }
}
