namespace Saral.FileProcessor.Core.Abstractions;

public interface IFileLoader
{
    FileLoadContext Load(string filePath);
    FileLoadContext Load(Stream stream, string fileName);
}