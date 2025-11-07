using System.Globalization;
using Application.ProductCategories;
using CsvHelper;
using CsvHelper.Configuration;

namespace Infrastructure.Services;

internal sealed class CsvParsingService : ICsvParsingService
{
    public async Task<List<CsvRowData>> ParseCsvAsync(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        });

        var records = new List<CsvRowData>();
        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            var record = new CsvRowData
            {
                Type = csv.GetField("type")?.Trim() ?? string.Empty,
                ProductCategory = csv.GetField("product_category")?.Trim() ?? string.Empty,
                Segment = csv.GetField("segment")?.Trim() ?? string.Empty
            };

            records.Add(record);
        }

        return records;
    }
}