using Application.Abstractions.Exporting;
using ClosedXML.Excel;

namespace Infrastructure.Exporting;

public class ExcelExportService<T> : IExportService<T>
{
    public async Task<byte[]> ExportAsync(IEnumerable<T> data,
                                          Dictionary<string, Func<T, object>> columnMappings,
                                          CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook();
        IXLWorksheet worksheet = workbook.Worksheets.Add("Export");

        // Write headers
        int colIndex = 1;
        foreach (string header in columnMappings.Keys)
        {
            worksheet.Cell(1, colIndex).Value = header;
            colIndex++;
        }

        // Write rows
        int rowIndex = 2;
        foreach (T? item in data)
        {
            colIndex = 1;
            foreach (Func<T, object> map in columnMappings.Values)
            {
                worksheet.Cell(rowIndex, colIndex).Value = map(item)?.ToString();
                colIndex++;
            }
            rowIndex++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return await Task.FromResult(stream.ToArray());
    }
}
