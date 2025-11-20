using System.Globalization;
using Application.Abstractions.Parsing;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Infrastructure.Services;

internal sealed class ExcelCashFlowParser(ILogger<ExcelCashFlowParser> logger) : IExcelCashFlowParser
{
    public async Task<Result<List<ParsedCashFlow>>> ParseCashFlowsAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                logger.LogError("Excel file not found: {FilePath}", filePath);
                return Result.Failure<List<ParsedCashFlow>>(
                    Error.NotFound("ExcelFile.NotFound", $"File not found: {filePath}"));
            }

            return await Task.Run(() => ParseExcelFile(filePath), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing Excel file: {FilePath}", filePath);
            return Result.Failure<List<ParsedCashFlow>>(
                Error.Failure("ExcelParsing.Error", $"Error parsing Excel file: {ex.Message}"));
        }
    }

    private Result<List<ParsedCashFlow>> ParseExcelFile(string filePath)
    {
        using XLWorkbook workbook = new(filePath);
        IXLWorksheet worksheet = workbook.Worksheet(1);

        var cashFlows = new List<ParsedCashFlow>();

        int? monthColumnIndex = null;
        int? cashFlowColumnIndex = null;

        IXLRow headerRow = worksheet.Row(1);
        foreach (IXLCell cell in headerRow.CellsUsed())
        {
#pragma warning disable CA1304 // Specify CultureInfo
            string headerValue = cell.Value.ToString().Trim().ToUpperInvariant();
#pragma warning restore CA1304 // Specify CultureInfo

            if (headerValue == "MONTH")
            {
                monthColumnIndex = cell.Address.ColumnNumber;
            }
            else if (headerValue == "CF" ||
                     headerValue == "CASHFLOW" ||
                     headerValue == "CASH FLOW")
            {
                cashFlowColumnIndex = cell.Address.ColumnNumber;
            }
        }

        if (!monthColumnIndex.HasValue)
        {
            logger.LogError("Month column not found in Excel file");
            return Result.Failure<List<ParsedCashFlow>>(
                Error.Validation("ExcelParsing.MissingColumn",
                    "Required column 'Month' not found in Excel file"));
        }

        if (!cashFlowColumnIndex.HasValue)
        {
            logger.LogError("Cash flow column not found in Excel file");
            return Result.Failure<List<ParsedCashFlow>>(
                Error.Validation("ExcelParsing.MissingColumn",
                    "Required column 'CF', 'CashFlow', or 'Cash Flow' not found in Excel file"));
        }

        int rowCount = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = 2; row <= rowCount; row++)
        {
            IXLCell monthCell = worksheet.Cell(row, monthColumnIndex.Value);
            IXLCell cashFlowCell = worksheet.Cell(row, cashFlowColumnIndex.Value);

            if (monthCell.IsEmpty() || cashFlowCell.IsEmpty())
            {
                continue;
            }

#pragma warning disable CA1304 // Specify CultureInfo
            if (!int.TryParse(monthCell.Value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int month))
            {
                logger.LogWarning("Invalid month value at row {Row}: {Value}", row, monthCell.Value);
                continue;
            }
#pragma warning restore CA1304 // Specify CultureInfo

#pragma warning disable CA1304 // Specify CultureInfo
            if (!decimal.TryParse(cashFlowCell.Value.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal cashFlow))
            {
                logger.LogWarning("Invalid cash flow value at row {Row}: {Value}", row, cashFlowCell.Value);
                continue;
            }
#pragma warning restore CA1304 // Specify CultureInfo

            cashFlows.Add(new ParsedCashFlow(month, cashFlow));
        }

        if (cashFlows.Count == 0)
        {
            logger.LogError("No valid cash flows found in Excel file");
            return Result.Failure<List<ParsedCashFlow>>(
                Error.Validation("ExcelParsing.NoData",
                    "No valid cash flow data found in Excel file"));
        }

        logger.LogInformation("Successfully parsed {Count} cash flows from Excel file", cashFlows.Count);
        return Result.Success(cashFlows);
    }
}
