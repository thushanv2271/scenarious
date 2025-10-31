using Application.Abstractions.Exporting;
using OfficeOpenXml;

namespace Infrastructure.Exporting;

public class ExcelExportService<T> : IExportService<T>
{
    public async Task<byte[]> ExportAsync(IEnumerable<T> data,
                                          Dictionary<string, Func<T, object>> columnMappings,
                                          CancellationToken cancellationToken)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Export");

        // Write headers
        int colIndex = 1;
        foreach (string header in columnMappings.Keys)
        {
            worksheet.Cells[1, colIndex].Value = header;
            colIndex++;
        }

        // Write rows
        int rowIndex = 2;
        foreach (T? item in data)
        {
            colIndex = 1;
            foreach (Func<T, object> map in columnMappings.Values)
            {
                worksheet.Cells[rowIndex, colIndex].Value = map(item)?.ToString();
                colIndex++;
            }
            rowIndex++;
        }

        worksheet.Cells.AutoFitColumns();

        return await package.GetAsByteArrayAsync(cancellationToken);
    }
}
