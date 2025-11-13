using Application.Models;
using Domain.PDCalculation;
using System.Globalization;
using System.Text;

namespace Infrastructure.PDCalculationSteps.Helpers;

/// <summary>
/// High-performance CSV reader optimized for PD calculation data processing
/// </summary>
public static class FastCsvReader
{
    /// <summary>
    /// Reads loan data from a CSV file with high performance
    /// </summary>
    /// <param name="filePath">Path to the CSV file</param>
    /// <param name="fileDetailsId">ID of the file details record</param>
    /// <param name="quarterEndDate">Quarter end date for calculations</param>
    /// <param name="frequency">Frequency type for remaining maturity calculation</param>
    /// <param name="buckets">Date passed due bucket configurations</param>
    /// <returns>List of loan details creation requests</returns>
    public static List<LoanDetailsCreationRequest> ReadLoanDataFromCsv(
        string filePath,
        Guid fileDetailsId,
        DateTime quarterEndDate,
        FrequencyType frequency,
        List<DatePassedDueBucket> buckets)
    {
        List<LoanDetailsCreationRequest> loanDetailsList = new();

        // Use StreamReader for better performance with large files
        using StreamReader reader = new(filePath, Encoding.UTF8);

        // Read header line
        string? headerLine = reader.ReadLine();
        if (headerLine is null)
        {
            return loanDetailsList; // Empty file
        }

        // Parse headers and create column mapping
        string[] headers = ParseCsvLine(headerLine);
        Dictionary<string, int> columnMapping = CreateColumnMapping(headers);

        // Validate required columns exist
        if (!ValidateRequiredColumns(columnMapping))
        {
            throw new InvalidOperationException("Required columns are missing from the CSV file");
        }

        // Read data lines
        string? line;
        int lineNumber = 1;

        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue; // Skip empty lines
            }

            try
            {
                string[] values = ParseCsvLine(line);
                if (values.Length == 0)
                {
                    continue; // Skip empty rows
                }

                LoanDetailsCreationRequest loanDetails = ExtractLoanDetailsFromCsvRow(
                    values,
                    columnMapping,
                    fileDetailsId,
                    quarterEndDate,
                    frequency,
                    buckets);

                loanDetailsList.Add(loanDetails);
            }
            catch (Exception ex)
            {
                // Log the error but continue processing other rows
                Console.WriteLine($"Error processing CSV row {lineNumber}: {ex.Message}");
            }
        }

        return loanDetailsList;
    }

    /// <summary>
    /// Parses a CSV line with optimized performance, handling quoted values
    /// </summary>
    /// <param name="line">The CSV line to parse</param>
    /// <returns>Array of values</returns>
    private static string[] ParseCsvLine(string line)
    {
        List<string> result = new();
        bool inQuotes = false;
        StringBuilder currentField = new();

        ReadOnlySpan<char> span = line.AsSpan();

        int i = 0;
        while (i < span.Length)
        {
            char c = span[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < span.Length && span[i + 1] == '"')
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

        return result.ToArray();
    }

    /// <summary>
    /// Creates a column mapping from CSV headers
    /// </summary>
    /// <param name="headers">Array of header values</param>
    /// <returns>Dictionary mapping column names to positions</returns>
    private static Dictionary<string, int> CreateColumnMapping(string[] headers)
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
    /// Validates that all required columns are present in the CSV
    /// </summary>
    /// <param name="columnMapping">Column mapping to validate</param>
    /// <returns>True if all required columns are present</returns>
    private static bool ValidateRequiredColumns(Dictionary<string, int> columnMapping)
    {
        string[] requiredColumns = {
            "Customer Number",
            "Facility number",
            "Grant date",
            "Maturity date/ Expiry Date",
            "Days Past Due"
        };

        return requiredColumns.All(col => columnMapping.ContainsKey(col));
    }

    /// <summary>
    /// Extracts loan details from a CSV row with optimized performance
    /// </summary>
    /// <param name="values">Array of values from the CSV row</param>
    /// <param name="columnMapping">Column name to position mapping</param>
    /// <param name="fileDetailsId">ID of the file details record</param>
    /// <param name="quarterEndDate">Quarter end date for calculations</param>
    /// <param name="frequency">Frequency type for remaining maturity calculation</param>
    /// <param name="buckets">Date passed due bucket configurations</param>
    /// <returns>Loan details creation request</returns>
    private static LoanDetailsCreationRequest ExtractLoanDetailsFromCsvRow(
        string[] values,
        Dictionary<string, int> columnMapping,
        Guid fileDetailsId,
        DateTime quarterEndDate,
        FrequencyType frequency,
        List<DatePassedDueBucket> buckets)
    {
        // Extract basic data from CSV with direct array access
        string customerNumber = GetValue(values, columnMapping, "Customer Number");
        string facilityNumber = GetValue(values, columnMapping, "Facility number");
        string branch = GetValue(values, columnMapping, "Branch");
        string productCategory = GetValue(values, columnMapping, "Product category");
        string segment = GetValue(values, columnMapping, "Segment");
        string industry = GetValue(values, columnMapping, "Industry");
        string earningType = GetValue(values, columnMapping, "Earning Type");
        string nature = GetValue(values, columnMapping, "Nature");

        DateTime grantDate = GetDateValue(values, columnMapping, "Grant date");
        DateTime maturityDate = GetDateValue(values, columnMapping, "Maturity date/ Expiry Date");

        decimal interestRate = GetDecimalValue(values, columnMapping, "Interest Rate");
        string installmentType = GetValue(values, columnMapping, "Installment Type (Monthly/ Quarterly/ Weekly/ Daily/ Annually/ Bullet)");
        int daysPastDue = GetIntValue(values, columnMapping, "Days Past Due");
        decimal limit = GetDecimalValue(values, columnMapping, "Limit");
        decimal totalOS = GetDecimalValue(values, columnMapping, "Total OS");
        decimal undisbursedAmount = GetDecimalValue(values, columnMapping, "Undisbursed Amount");
        decimal interestInSuspense = GetDecimalValue(values, columnMapping, "Interest in Suspense");
        string collateralType = GetValue(values, columnMapping, "Collateral Type");
        decimal collateralValue = GetDecimalValue(values, columnMapping, "Collateral Value");

        bool rescheduled = GetBooleanValue(values, columnMapping, "Rescheduled (Yes/No)");
        bool restructured = GetBooleanValue(values, columnMapping, "Restructured (Yes/No)");
        int noOfTimesRestructured = GetIntValue(values, columnMapping, "No. of Times Restructured");
        bool upgradedToDelinquencyBucket = GetBooleanValue(values, columnMapping, "Upgraded to delinquency bucket (Yes/No)");
        bool individuallyImpaired = GetBooleanValue(values, columnMapping, "Individually Impaired (Yes/No)");
        string bucketingInIndividualAssessment = GetValue(values, columnMapping, "Bucketing in Individual Assessment");
        string period = GetValue(values, columnMapping, "Period");

        // Calculate remaining maturity and bucket label
        int remainingMaturityYears = CalculationHelper.CalculateRemainingMaturity(maturityDate, quarterEndDate, frequency);
        string bucketLabel = CalculationHelper.DetermineBucketLabel(daysPastDue, buckets);

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

    #region Optimized Value Extraction Methods

    /// <summary>
    /// Gets a string value with direct array access for performance
    /// </summary>
    private static string GetValue(string[] values, Dictionary<string, int> columnMapping, string columnName)
    {
        if (columnMapping.TryGetValue(columnName, out int columnIndex) && columnIndex < values.Length)
        {
            return values[columnIndex];
        }
        return string.Empty;
    }

    /// <summary>
    /// Gets a decimal value with optimized parsing
    /// </summary>
    private static decimal GetDecimalValue(string[] values, Dictionary<string, int> columnMapping, string columnName)
    {
        string stringValue = GetValue(values, columnMapping, columnName);
        if (string.IsNullOrEmpty(stringValue))
        {
            return 0m;
        }

        return decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value) ? value : 0m;
    }

    /// <summary>
    /// Gets an integer value with optimized parsing
    /// </summary>
    private static int GetIntValue(string[] values, Dictionary<string, int> columnMapping, string columnName)
    {
        string stringValue = GetValue(values, columnMapping, columnName);
        if (string.IsNullOrEmpty(stringValue))
        {
            return 0;
        }

        return int.TryParse(stringValue, out int value) ? value : 0;
    }

    /// <summary>
    /// Gets a DateTime value with optimized parsing and UTC conversion
    /// </summary>
    private static DateTime GetDateValue(string[] values, Dictionary<string, int> columnMapping, string columnName)
    {
        string stringValue = GetValue(values, columnMapping, columnName);
        if (string.IsNullOrEmpty(stringValue))
        {
            return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        }

        // Optimized date parsing - try most common format first
        if (DateTime.TryParseExact(stringValue, "M/d/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedValue))
        {
            return DateTime.SpecifyKind(parsedValue, DateTimeKind.Utc);
        }

        // Try other common formats
        string[] dateFormats = {
            "MM/dd/yyyy",   // 01/13/2023
            "M/dd/yyyy",    // 1/13/2023
            "MM/d/yyyy",    // 01/3/2023
            "yyyy-MM-dd",   // 2023-01-13
            "dd/MM/yyyy",   // 13/01/2023
            "yyyy/MM/dd"    // 2023/01/13
        };

        foreach (string format in dateFormats)
        {
            if (DateTime.TryParseExact(stringValue, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime formatParsedValue))
            {
                return DateTime.SpecifyKind(formatParsedValue, DateTimeKind.Utc);
            }
        }

        // Last resort: General DateTime parsing
        if (DateTime.TryParse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime generalParsedValue))
        {
            return DateTime.SpecifyKind(generalParsedValue, DateTimeKind.Utc);
        }

        return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
    }

    /// <summary>
    /// Gets a boolean value with optimized parsing (Yes/No format)
    /// </summary>
    private static bool GetBooleanValue(string[] values, Dictionary<string, int> columnMapping, string columnName)
    {
        string value = GetValue(values, columnMapping, columnName);
        return string.Equals(value, "Yes", StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
