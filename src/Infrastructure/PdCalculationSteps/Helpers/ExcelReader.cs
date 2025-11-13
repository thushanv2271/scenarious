using ClosedXML.Excel;
using Domain.PDCalculation;
using Infrastructure.PDCalculationSteps.Helpers;
using Application.Models;
using System.Globalization;
using System.Text;

namespace Infrastructure.PDCalculationSteps.Helpers;

/// <summary>
/// Helper class for reading Excel and CSV files and extracting loan data
/// </summary>
public static class ExcelReader
{
    /// <summary>
    /// Reads loan data from an Excel or CSV file
    /// </summary>
    /// <param name="filePath">Path to the Excel or CSV file</param>
    /// <param name="fileDetailsId">ID of the file details record</param>
    /// <param name="quarterEndDate">Quarter end date for calculations</param>
    /// <param name="frequency">Frequency type for remaining maturity calculation</param>
    /// <returns>List of loan details creation requests</returns>
    public static List<LoanDetailsCreationRequest> ReadLoanDataFromExcel(
        string filePath,
        Guid fileDetailsId,
        DateTime quarterEndDate,
        FrequencyType frequency)
    {
        string extension = Path.GetExtension(filePath).ToUpperInvariant();

        return extension switch
        {
            ".CSV" => ReadLoanDataFromCsv(filePath, fileDetailsId, quarterEndDate, frequency),
            ".XLSX" or ".XLSM" or ".XLTX" or ".XLTM" => ReadLoanDataFromExcelFile(filePath, fileDetailsId, quarterEndDate, frequency),
            _ => throw new NotSupportedException($"File extension '{extension}' is not supported. Supported extensions are '.csv', '.xlsx', '.xlsm', '.xltx' and '.xltm'.")
        };
    }

    /// <summary>
    /// Reads loan data from a CSV file
    /// </summary>
    private static List<LoanDetailsCreationRequest> ReadLoanDataFromCsv(
        string filePath,
        Guid fileDetailsId,
        DateTime quarterEndDate,
        FrequencyType frequency)
    {
        List<LoanDetailsCreationRequest> loanDetailsList = [];

        string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
        if (lines.Length < 2)
        {
            return loanDetailsList; // No data rows
        }

        // Parse header row
        string[] headers = ParseCsvLine(lines[0]);
        Dictionary<string, int> columnMapping = CreateColumnMappingFromHeaders(headers);

        // Parse data rows
        for (int i = 1; i < lines.Length; i++)
        {
            try
            {
                string[] values = ParseCsvLine(lines[i]);
                if (values.Length == 0)
                {
                    continue; // Skip empty rows
                }

                LoanDetailsCreationRequest loanDetails = ExtractLoanDetailsFromCsvRow(
                    values,
                    columnMapping,
                    fileDetailsId,
                    quarterEndDate,
                    frequency);

                loanDetailsList.Add(loanDetails);
            }
            catch (Exception ex)
            {
                // Log the error but continue processing other rows
                Console.WriteLine($"Error processing CSV row {i + 1}: {ex.Message}");
            }
        }

        return loanDetailsList;
    }

    /// <summary>
    /// Reads loan data from an Excel file
    /// </summary>
    private static List<LoanDetailsCreationRequest> ReadLoanDataFromExcelFile(
        string filePath,
        Guid fileDetailsId,
        DateTime quarterEndDate,
        FrequencyType frequency)
    {
        List<LoanDetailsCreationRequest> loanDetailsList = [];

        using XLWorkbook workbook = new(filePath);
        IXLWorksheet worksheet = workbook.Worksheet(1); // Get the first worksheet

        // Find the header row (assuming it's the first row)
        IXLRow headerRow = worksheet.Row(1);
        Dictionary<string, int> columnMapping = CreateColumnMapping(headerRow);

        // Read data starting from row 2
        int lastRowWithData = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int rowNumber = 2; rowNumber <= lastRowWithData; rowNumber++)
        {
            IXLRow dataRow = worksheet.Row(rowNumber);

            try
            {
                LoanDetailsCreationRequest loanDetails = ExtractLoanDetailsFromRow(
                    dataRow,
                    columnMapping,
                    fileDetailsId,
                    quarterEndDate,
                    frequency);

                loanDetailsList.Add(loanDetails);
            }
            catch (Exception ex)
            {
                // Log the error but continue processing other rows
                // In a real implementation, you might want to use proper logging
                Console.WriteLine($"Error processing row {rowNumber}: {ex.Message}");
            }
        }

        return loanDetailsList;
    }

    /// <summary>
    /// Creates a mapping of column names to their positions
    /// </summary>
    /// <param name="headerRow">The header row from the Excel file</param>
    /// <returns>Dictionary mapping column names to positions</returns>
    private static Dictionary<string, int> CreateColumnMapping(IXLRow headerRow)
    {
        Dictionary<string, int> mapping = new(StringComparer.OrdinalIgnoreCase);

        foreach (IXLCell cell in headerRow.CellsUsed())
        {
            string cellValue = cell.GetString().Trim();
            if (!string.IsNullOrEmpty(cellValue))
            {
                mapping[cellValue] = cell.Address.ColumnNumber;
            }
        }

        return mapping;
    }

    /// <summary>
    /// Extracts loan details from a data row
    /// </summary>
    /// <param name="dataRow">The data row to extract from</param>
    /// <param name="columnMapping">Column name to position mapping</param>
    /// <param name="fileDetailsId">ID of the file details record</param>
    /// <param name="quarterEndDate">Quarter end date for calculations</param>
    /// <param name="frequency">Frequency type for remaining maturity calculation</param>
    /// <returns>Loan details creation request</returns>
    private static LoanDetailsCreationRequest ExtractLoanDetailsFromRow(
        IXLRow dataRow,
        Dictionary<string, int> columnMapping,
        Guid fileDetailsId,
        DateTime quarterEndDate,
        FrequencyType frequency)
    {
        // Extract basic data from Excel
        string customerNumber = GetCellValue(dataRow, columnMapping, "Customer Number");
        string facilityNumber = GetCellValue(dataRow, columnMapping, "Facility number");
        string branch = GetCellValue(dataRow, columnMapping, "Branch");
        string productCategory = GetCellValue(dataRow, columnMapping, "Product category");
        string segment = GetCellValue(dataRow, columnMapping, "Segment");
        string industry = GetCellValue(dataRow, columnMapping, "Industry");
        string earningType = GetCellValue(dataRow, columnMapping, "Earning Type");
        string nature = GetCellValue(dataRow, columnMapping, "Nature");

        DateTime grantDate = GetDateValue(dataRow, columnMapping, "Grant date");
        DateTime maturityDate = GetDateValue(dataRow, columnMapping, "Maturity date/ Expiry Date");

        decimal interestRate = GetDecimalValue(dataRow, columnMapping, "Interest Rate");
        string installmentType = GetCellValue(dataRow, columnMapping, "Installment Type (Monthly/ Quarterly/ Weekly/ Daily/ Annually/ Bullet)");
        int daysPastDue = GetIntValue(dataRow, columnMapping, "Days Past Due");
        decimal limit = GetDecimalValue(dataRow, columnMapping, "Limit");
        decimal totalOS = GetDecimalValue(dataRow, columnMapping, "Total OS");
        decimal undisbursedAmount = GetDecimalValue(dataRow, columnMapping, "Undisbursed Amount");
        decimal interestInSuspense = GetDecimalValue(dataRow, columnMapping, "Interest in Suspense");
        string collateralType = GetCellValue(dataRow, columnMapping, "Collateral Type");
        decimal collateralValue = GetDecimalValue(dataRow, columnMapping, "Collateral Value");

        bool rescheduled = GetBooleanValue(dataRow, columnMapping, "Rescheduled (Yes/No)");
        bool restructured = GetBooleanValue(dataRow, columnMapping, "Restructured (Yes/No)");
        int noOfTimesRestructured = GetIntValue(dataRow, columnMapping, "No. of Times Restructured");
        bool upgradedToDelinquencyBucket = GetBooleanValue(dataRow, columnMapping, "Upgraded to delinquency bucket (Yes/No)");
        bool individuallyImpaired = GetBooleanValue(dataRow, columnMapping, "Individually Impaired (Yes/No)");
        string bucketingInIndividualAssessment = GetCellValue(dataRow, columnMapping, "Bucketing in Individual Assessment");
        string period = GetCellValue(dataRow, columnMapping, "Period");

        // Calculate remaining maturity and bucket label (requires bucket configuration)
        int remainingMaturityYears = CalculationHelper.CalculateRemainingMaturity(maturityDate, quarterEndDate, frequency);
        string bucketLabel = "Unknown Bucket"; // TODO: Add bucket configuration parameter

        return new LoanDetailsCreationRequest(
            fileDetailsId,
            customerNumber,
            facilityNumber,
            branch,
            productCategory,
            segment,
            industry,
            earningType,
            nature,
            grantDate,
            maturityDate,
            interestRate,
            installmentType,
            daysPastDue,
            limit,
            totalOS,
            undisbursedAmount,
            interestInSuspense,
            collateralType,
            collateralValue,
            rescheduled,
            restructured,
            noOfTimesRestructured,
            upgradedToDelinquencyBucket,
            individuallyImpaired,
            bucketingInIndividualAssessment,
            period,
            remainingMaturityYears,
            bucketLabel,
            string.Empty); // FinalBucket - initially empty, calculated later
    }

    /// <summary>
    /// Gets a string value from a cell
    /// </summary>
    /// <param name="row">The row to read from</param>
    /// <param name="columnMapping">Column mapping</param>
    /// <param name="columnName">Name of the column</param>
    /// <returns>String value or empty string if not found</returns>
    private static string GetCellValue(IXLRow row, Dictionary<string, int> columnMapping, string columnName)
    {
        if (columnMapping.TryGetValue(columnName, out int columnIndex))
        {
            return row.Cell(columnIndex).GetString().Trim();
        }
        return string.Empty;
    }

    /// <summary>
    /// Gets a decimal value from a cell
    /// </summary>
    /// <param name="row">The row to read from</param>
    /// <param name="columnMapping">Column mapping</param>
    /// <param name="columnName">Name of the column</param>
    /// <returns>Decimal value or 0 if not found or invalid</returns>
    private static decimal GetDecimalValue(IXLRow row, Dictionary<string, int> columnMapping, string columnName)
    {
        if (columnMapping.TryGetValue(columnName, out int columnIndex))
        {
            IXLCell cell = row.Cell(columnIndex);
            if (cell.TryGetValue(out decimal value))
            {
                return value;
            }

            string stringValue = cell.GetString().Trim();
            if (decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedValue))
            {
                return parsedValue;
            }
        }
        return 0m;
    }

    /// <summary>
    /// Gets an integer value from a cell
    /// </summary>
    /// <param name="row">The row to read from</param>
    /// <param name="columnMapping">Column mapping</param>
    /// <param name="columnName">Name of the column</param>
    /// <returns>Integer value or 0 if not found or invalid</returns>
    private static int GetIntValue(IXLRow row, Dictionary<string, int> columnMapping, string columnName)
    {
        if (columnMapping.TryGetValue(columnName, out int columnIndex))
        {
            IXLCell cell = row.Cell(columnIndex);
            if (cell.TryGetValue(out int value))
            {
                return value;
            }

            string stringValue = cell.GetString().Trim();
            if (int.TryParse(stringValue, out int parsedValue))
            {
                return parsedValue;
            }
        }
        return 0;
    }

    /// <summary>
    /// Gets a DateTime value from a cell
    /// </summary>
    /// <param name="row">The row to read from</param>
    /// <param name="columnMapping">Column mapping</param>
    /// <param name="columnName">Name of the column</param>
    /// <returns>DateTime value or DateTime.MinValue if not found or invalid</returns>
    private static DateTime GetDateValue(IXLRow row, Dictionary<string, int> columnMapping, string columnName)
    {
        if (columnMapping.TryGetValue(columnName, out int columnIndex))
        {
            IXLCell cell = row.Cell(columnIndex);

            // First try: Excel native DateTime value
            if (cell.TryGetValue(out DateTime value))
            {
                // Ensure the date is UTC for PostgreSQL compatibility
                return value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
                    : value.ToUniversalTime();
            }

            // Second try: Parse string value with multiple formats
            string stringValue = cell.GetString().Trim();
            if (!string.IsNullOrEmpty(stringValue))
            {
                // Common date formats to try
                string[] dateFormats = {
                    "M/d/yyyy",     // 1/13/2023
                    "MM/dd/yyyy",   // 01/13/2023
                    "M/dd/yyyy",    // 1/13/2023
                    "MM/d/yyyy",    // 01/3/2023
                    "yyyy-MM-dd",   // 2023-01-13
                    "dd/MM/yyyy",   // 13/01/2023
                    "yyyy/MM/dd"    // 2023/01/13
                };

                foreach (string format in dateFormats)
                {
                    if (DateTime.TryParseExact(stringValue, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedValue))
                    {
                        // Ensure parsed date is UTC
                        return DateTime.SpecifyKind(parsedValue, DateTimeKind.Utc);
                    }
                }

                // Last try: General DateTime parsing
                if (DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime generalParsedValue))
                {
                    // Ensure parsed date is UTC
                    return DateTime.SpecifyKind(generalParsedValue, DateTimeKind.Utc);
                }
            }
        }
        return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
    }

    /// <summary>
    /// Gets a boolean value from a cell (Yes/No format)
    /// </summary>
    /// <param name="row">The row to read from</param>
    /// <param name="columnMapping">Column mapping</param>
    /// <param name="columnName">Name of the column</param>
    /// <returns>Boolean value (true for "Yes", false otherwise)</returns>
    private static bool GetBooleanValue(IXLRow row, Dictionary<string, int> columnMapping, string columnName)
    {
        string value = GetCellValue(row, columnMapping, columnName);
        return string.Equals(value, "Yes", StringComparison.OrdinalIgnoreCase);
    }

    #region CSV Helper Methods

    /// <summary>
    /// Parses a CSV line, handling quoted values
    /// </summary>
    /// <param name="line">The CSV line to parse</param>
    /// <returns>Array of values</returns>
    private static string[] ParseCsvLine(string line)
    {
        List<string> result = [];
        bool inQuotes = false;
        StringBuilder currentField = new();

        int i = 0;
        while (i < line.Length)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Escaped quote
                    currentField.Append('"');
                    i += 2; // Skip both quotes
                }
                else
                {
                    // Toggle quote state
                    inQuotes = !inQuotes;
                    i++;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                // Field separator
                result.Add(currentField.ToString().Trim());
                currentField.Clear();
                i++;
            }
            else
            {
                currentField.Append(c);
                i++;
            }
        }

        // Add the last field
        result.Add(currentField.ToString().Trim());

        return [.. result];
    }

    /// <summary>
    /// Creates a column mapping from CSV headers
    /// </summary>
    /// <param name="headers">Array of header values</param>
    /// <returns>Dictionary mapping column names to positions</returns>
    private static Dictionary<string, int> CreateColumnMappingFromHeaders(string[] headers)
    {
        Dictionary<string, int> mapping = new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < headers.Length; i++)
        {
            string header = headers[i].Trim();
            if (!string.IsNullOrEmpty(header))
            {
                mapping[header] = i;
            }
        }

        return mapping;
    }

    /// <summary>
    /// Extracts loan details from a CSV row
    /// </summary>
    /// <param name="values">Array of values from the CSV row</param>
    /// <param name="columnMapping">Column name to position mapping</param>
    /// <param name="fileDetailsId">ID of the file details record</param>
    /// <param name="quarterEndDate">Quarter end date for calculations</param>
    /// <param name="frequency">Frequency type for remaining maturity calculation</param>
    /// <returns>Loan details creation request</returns>
    private static LoanDetailsCreationRequest ExtractLoanDetailsFromCsvRow(
        string[] values,
        Dictionary<string, int> columnMapping,
        Guid fileDetailsId,
        DateTime quarterEndDate,
        FrequencyType frequency)
    {
        // Extract basic data from CSV
        string customerNumber = GetCsvValue(values, columnMapping, "Customer Number");
        string facilityNumber = GetCsvValue(values, columnMapping, "Facility number");
        string branch = GetCsvValue(values, columnMapping, "Branch");
        string productCategory = GetCsvValue(values, columnMapping, "Product category");
        string segment = GetCsvValue(values, columnMapping, "Segment");
        string industry = GetCsvValue(values, columnMapping, "Industry");
        string earningType = GetCsvValue(values, columnMapping, "Earning Type");
        string nature = GetCsvValue(values, columnMapping, "Nature");

        DateTime grantDate = GetCsvDateValue(values, columnMapping, "Grant date");
        DateTime maturityDate = GetCsvDateValue(values, columnMapping, "Maturity date/ Expiry Date");

        decimal interestRate = GetCsvDecimalValue(values, columnMapping, "Interest Rate");
        string installmentType = GetCsvValue(values, columnMapping, "Installment Type (Monthly/ Quarterly/ Weekly/ Daily/ Annually/ Bullet)");
        int daysPastDue = GetCsvIntValue(values, columnMapping, "Days Past Due");
        decimal limit = GetCsvDecimalValue(values, columnMapping, "Limit");
        decimal totalOS = GetCsvDecimalValue(values, columnMapping, "Total OS");
        decimal undisbursedAmount = GetCsvDecimalValue(values, columnMapping, "Undisbursed Amount");
        decimal interestInSuspense = GetCsvDecimalValue(values, columnMapping, "Interest in Suspense");
        string collateralType = GetCsvValue(values, columnMapping, "Collateral Type");
        decimal collateralValue = GetCsvDecimalValue(values, columnMapping, "Collateral Value");

        bool rescheduled = GetCsvBooleanValue(values, columnMapping, "Rescheduled (Yes/No)");
        bool restructured = GetCsvBooleanValue(values, columnMapping, "Restructured (Yes/No)");
        int noOfTimesRestructured = GetCsvIntValue(values, columnMapping, "No. of Times Restructured");
        bool upgradedToDelinquencyBucket = GetCsvBooleanValue(values, columnMapping, "Upgraded to delinquency bucket (Yes/No)");
        bool individuallyImpaired = GetCsvBooleanValue(values, columnMapping, "Individually Impaired (Yes/No)");
        string bucketingInIndividualAssessment = GetCsvValue(values, columnMapping, "Bucketing in Individual Assessment");
        string period = GetCsvValue(values, columnMapping, "Period");

        // Calculate remaining maturity and bucket label (requires bucket configuration)
        int remainingMaturityYears = CalculationHelper.CalculateRemainingMaturity(maturityDate, quarterEndDate, frequency);
        string bucketLabel = "Unknown Bucket"; // TODO: Add bucket configuration parameter

        return new LoanDetailsCreationRequest(
            fileDetailsId,
            customerNumber,
            facilityNumber,
            branch,
            productCategory,
            segment,
            industry,
            earningType,
            nature,
            grantDate,
            maturityDate,
            interestRate,
            installmentType,
            daysPastDue,
            limit,
            totalOS,
            undisbursedAmount,
            interestInSuspense,
            collateralType,
            collateralValue,
            rescheduled,
            restructured,
            noOfTimesRestructured,
            upgradedToDelinquencyBucket,
            individuallyImpaired,
            bucketingInIndividualAssessment,
            period,
            remainingMaturityYears,
            bucketLabel,
            string.Empty); // FinalBucket - initially empty, calculated later
    }

    /// <summary>
    /// Gets a string value from CSV values array
    /// </summary>
    /// <param name="values">Values array</param>
    /// <param name="columnMapping">Column mapping</param>
    /// <param name="columnName">Name of the column</param>
    /// <returns>String value or empty string if not found</returns>
    private static string GetCsvValue(string[] values, Dictionary<string, int> columnMapping, string columnName)
    {
        if (columnMapping.TryGetValue(columnName, out int columnIndex) && columnIndex < values.Length)
        {
            return values[columnIndex].Trim();
        }
        return string.Empty;
    }

    /// <summary>
    /// Gets a decimal value from CSV values array
    /// </summary>
    /// <param name="values">Values array</param>
    /// <param name="columnMapping">Column mapping</param>
    /// <param name="columnName">Name of the column</param>
    /// <returns>Decimal value or 0 if not found or invalid</returns>
    private static decimal GetCsvDecimalValue(string[] values, Dictionary<string, int> columnMapping, string columnName)
    {
        string stringValue = GetCsvValue(values, columnMapping, columnName);
        if (decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value))
        {
            return value;
        }
        return 0m;
    }

    /// <summary>
    /// Gets an integer value from CSV values array
    /// </summary>
    /// <param name="values">Values array</param>
    /// <param name="columnMapping">Column mapping</param>
    /// <param name="columnName">Name of the column</param>
    /// <returns>Integer value or 0 if not found or invalid</returns>
    private static int GetCsvIntValue(string[] values, Dictionary<string, int> columnMapping, string columnName)
    {
        string stringValue = GetCsvValue(values, columnMapping, columnName);
        if (int.TryParse(stringValue, out int value))
        {
            return value;
        }
        return 0;
    }

    /// <summary>
    /// Gets a DateTime value from CSV values array
    /// </summary>
    /// <param name="values">Values array</param>
    /// <param name="columnMapping">Column mapping</param>
    /// <param name="columnName">Name of the column</param>
    /// <returns>DateTime value or DateTime.MinValue if not found or invalid</returns>
    private static DateTime GetCsvDateValue(string[] values, Dictionary<string, int> columnMapping, string columnName)
    {
        string stringValue = GetCsvValue(values, columnMapping, columnName);
        if (!string.IsNullOrEmpty(stringValue))
        {
            // Common date formats to try
            string[] dateFormats = {
                "M/d/yyyy",     // 1/13/2023
                "MM/dd/yyyy",   // 01/13/2023
                "M/dd/yyyy",    // 1/13/2023
                "MM/d/yyyy",    // 01/3/2023
                "yyyy-MM-dd",   // 2023-01-13
                "dd/MM/yyyy",   // 13/01/2023
                "yyyy/MM/dd"    // 2023/01/13
            };

            foreach (string format in dateFormats)
            {
                if (DateTime.TryParseExact(stringValue, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedValue))
                {
                    // Ensure parsed date is UTC
                    return DateTime.SpecifyKind(parsedValue, DateTimeKind.Utc);
                }
            }

            // Last try: General DateTime parsing
            if (DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime generalParsedValue))
            {
                // Ensure parsed date is UTC
                return DateTime.SpecifyKind(generalParsedValue, DateTimeKind.Utc);
            }
        }
        return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
    }

    /// <summary>
    /// Gets a boolean value from CSV values array (Yes/No format)
    /// </summary>
    /// <param name="values">Values array</param>
    /// <param name="columnMapping">Column mapping</param>
    /// <param name="columnName">Name of the column</param>
    /// <returns>Boolean value (true for "Yes", false otherwise)</returns>
    private static bool GetCsvBooleanValue(string[] values, Dictionary<string, int> columnMapping, string columnName)
    {
        string value = GetCsvValue(values, columnMapping, columnName);
        return string.Equals(value, "Yes", StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
