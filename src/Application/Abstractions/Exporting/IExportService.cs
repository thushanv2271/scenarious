namespace Application.Abstractions.Exporting;

public interface IExportService<T>
{
    Task<byte[]> ExportAsync(IEnumerable<T> data,
                             Dictionary<string, Func<T, object>> columnMappings,
                             CancellationToken cancellationToken);
}
