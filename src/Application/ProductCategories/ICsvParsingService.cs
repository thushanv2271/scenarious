namespace Application.ProductCategories;

public interface ICsvParsingService
{
    Task<List<CsvRowData>> ParseCsvAsync(Stream csvStream);
}