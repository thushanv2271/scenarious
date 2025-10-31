namespace Application.Abstractions.Storage;

public interface IStorageService
{
    Task<string> SaveAsync(byte[] fileBytes, string fileName, CancellationToken cancellationToken);
}
