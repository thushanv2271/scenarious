using Domain.LGDCalculation;
using System.Globalization;

namespace Infrastructure.LgdCalculationSteps.Helpers;

/// <summary>
/// High-performance CSV reader for LGD calculation files
/// </summary>
public static class LgdFastCsvReader
{
    /// <summary>
    /// Column name mappings for LGD files - maps database field names to possible CSV column name variations
    /// </summary>
    private static readonly Dictionary<string, string[]> LgdColumnMappings = new()
    {
        ["Customer Number"] = ["Customer Number", "CustomerNumber", "Customer_Number", "Cust_Number", "CustNumber"],
        ["Facility number"] = ["Facility number", "Facility Number", "FacilityNumber", "Facility_Number", "FacNo", "Facility_No"],
        ["Branch"] = ["Branch", "BranchName", "Branch_Name", "BranchCode", "Branch_Code"],
        ["Product category"] = ["Product category", "Product Category", "ProductCategory", "Product_Category", "ProdCategory", "Prod_Category"],
        ["Segment"] = ["Segment", "CustomerSegment", "Customer_Segment", "Cust_Segment"],
        ["Industry"] = ["Industry", "IndustryType", "Industry_Type", "IndustryCode", "Industry_Code"],
        ["Earning Type"] = ["Earning Type", "EarningType", "Earning_Type", "EarnType", "Earn_Type"],
        ["Nature"] = ["Nature", "NatureOfFacility", "Nature_Of_Facility", "FacilityNature", "Facility_Nature"],
        ["Grant date"] = ["Grant date", "Grant Date", "GrantDate", "Grant_Date", "DateGranted", "Date_Granted"],
        ["Maturity date/ Expiry Date"] = ["Maturity date/ Expiry Date", "Maturity Date", "MaturityDate", "Maturity_Date", "ExpiryDate", "Expiry_Date", "Expiry Date"],
        ["Interest Rate"] = ["Interest Rate", "InterestRate", "Interest_Rate", "Rate", "IntRate", "Int_Rate"],
        ["Installment Type (Monthly/ Quarterly/ Weekly/ Daily/ Annually/ Bullet)"] = ["Installment Type (Monthly/ Quarterly/ Weekly/ Daily/ Annually/ Bullet)", "Installment Type", "InstallmentType", "Installment_Type", "PaymentType", "Payment_Type"],
        ["Days Past Due"] = ["Days Past Due", "DaysPastDue", "Days_Past_Due", "DPD", "PastDueDays", "Past_Due_Days"],
        ["Limit"] = ["Limit", "CreditLimit", "Credit_Limit", "FacilityLimit", "Facility_Limit"],
        ["Total OS"] = ["Total OS", "TotalOS", "Total_OS", "Outstanding", "TotalOutstanding", "Total_Outstanding"],
        ["Undisbursed Amount"] = ["Undisbursed Amount", "UndisbursedAmount", "Undisbursed_Amount", "UndrawnAmount", "Undrawn_Amount"],
        ["Interest in Suspense"] = ["Interest in Suspense", "InterestInSuspense", "Interest_In_Suspense", "SuspenseInterest", "Suspense_Interest"],
        ["Collateral Type"] = ["Collateral Type", "CollateralType", "Collateral_Type", "SecurityType", "Security_Type"],
        ["Collateral Value"] = ["Collateral Value", "CollateralValue", "Collateral_Value", "SecurityValue", "Security_Value"],
        ["Rescheduled (Yes/No)"] = ["Rescheduled (Yes/No)", "Rescheduled", "IsRescheduled", "Is_Rescheduled"],
        ["Restructured (Yes/No)"] = ["Restructured (Yes/No)", "Restructured", "IsRestructured", "Is_Restructured"],
        ["No. of Times Restructured"] = ["No. of Times Restructured", "No of Times Restructured", "NoOfTimesRestructured", "No_Of_Times_Restructured", "TimesRestructured", "Times_Restructured"],
        ["Upgraded to delinquency bucket (Yes/No)"] = ["Upgraded to delinquency bucket (Yes/No)", "Upgraded to delinquency bucket", "UpgradedToDelinquencyBucket", "Upgraded_To_Delinquency_Bucket"],
        ["Individually Impaired (Yes/No)"] = ["Individually Impaired (Yes/No)", "Individually Impaired", "IndividuallyImpaired", "Individually_Impaired"],
        ["Bucketing in Individual Assessment"] = ["Bucketing in Individual Assessment", "BucketingInIndividualAssessment", "Bucketing_In_Individual_Assessment", "IndividualBucket", "Individual_Bucket"],
        ["Period"] = ["Period", "ReportingPeriod", "Reporting_Period", "AsAtDate", "As_At_Date"],
        ["First NPL Date"] = ["First NPL Date", "FirstNPLDate", "First_NPL_Date", "FirstNplDate", "First_Npl_Date"],
        ["Total Outstanding as at First NPL Date"] = ["Total Outstanding as at First NPL Date", "TotalOutstandingAsAtFirstNPLDate", "Total_Outstanding_As_At_First_NPL_Date", "OutstandingAtFirstNPL", "Outstanding_At_First_NPL"],
        ["Receipt Date"] = ["Receipt Date", "ReceiptDate", "Receipt_Date", "PaymentDate", "Payment_Date"],
        ["Closure Date"] = ["Closure Date", "ClosureDate", "Closure_Date", "AccountClosureDate", "Account_Closure_Date"],
        ["Cashflow"] = ["Cashflow", "CashFlow", "Cash_Flow", "CashFlows", "Cash_Flows"]
    };

    /// <summary>
    /// VC LGD specific column mappings (includes DPD column)
    /// </summary>
    private static readonly Dictionary<string, string[]> VcLgdColumnMappings = new(LgdColumnMappings)
    {
        ["DPD"] = ["DPD", "Days Past Due", "DaysPastDue", "Days_Past_Due", "PastDueDays", "Past_Due_Days"]
    };

    /// <summary>
    /// Reads LGD data from CSV file with high performance and flexible column mapping
    /// </summary>
    /// <param name="filePath">Path to the CSV file</param>
    /// <param name="lgdFileDetailsId">ID of the associated LgdFileDetails</param>
    /// <returns>List of LgdDetailsCreationRequest objects</returns>
    public static List<LgdDetailsCreationRequest> ReadLgdDataFromCsv(
        string filePath,
        Guid lgdFileDetailsId)
    {
        var result = new List<LgdDetailsCreationRequest>();

        using var reader = new StreamReader(filePath);

        // Read and parse header row
        string? headerLine = reader.ReadLine() ?? throw new InvalidOperationException("CSV file is empty or cannot be read.");

        // Parse headers and create column mapping
        string[] csvHeaders = ParseCsvFields(headerLine);
        Dictionary<string, int> columnIndexMap = CreateColumnIndexMap(csvHeaders, LgdColumnMappings);

        string? line;
        int lineNumber = 1; // Start from 1 since we already read the header

        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue; // Skip empty lines
            }

            // Skip lines that contain only commas or empty fields
            if (IsEmptyDataLine(line))
            {
                continue; // Skip lines with no meaningful data
            }

            try
            {
                LgdDetailsCreationRequest lgdDetails = ParseCsvLine(line, lgdFileDetailsId, columnIndexMap);
                result.Add(lgdDetails);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error parsing line {lineNumber} in CSV file: {ex.Message}", ex);
            }
        }

        return result;
    }

    /// <summary>
    /// Reads VC LGD data from CSV file with high performance and flexible column mapping
    /// </summary>
    /// <param name="filePath">Path to the CSV file</param>
    /// <param name="vcLgdFileDetailsId">ID of the associated VCLgdFileDetails</param>
    /// <returns>List of VCLgdDetailsCreationRequest objects</returns>
    public static List<VCLgdDetailsCreationRequest> ReadVCLgdDataFromCsv(
        string filePath,
        Guid vcLgdFileDetailsId)
    {
        var result = new List<VCLgdDetailsCreationRequest>();

        using var reader = new StreamReader(filePath);

        // Read and parse header row
        string? headerLine = reader.ReadLine() ?? throw new InvalidOperationException("CSV file is empty or cannot be read.");

        // Parse headers and create column mapping for VC LGD
        string[] csvHeaders = ParseCsvFields(headerLine);
        Dictionary<string, int> columnIndexMap = CreateColumnIndexMap(csvHeaders, VcLgdColumnMappings);

        string? line;
        int lineNumber = 1; // Start from 1 since we already read the header

        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue; // Skip empty lines
            }

            // Skip lines that contain only commas or empty fields
            if (IsEmptyDataLine(line))
            {
                continue; // Skip lines with no meaningful data
            }

            try
            {
                VCLgdDetailsCreationRequest vcLgdDetails = ParseVCLgdCsvLine(line, vcLgdFileDetailsId, columnIndexMap);
                result.Add(vcLgdDetails);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error parsing line {lineNumber} in VC LGD CSV file: {ex.Message}", ex);
            }
        }

        return result;
    }

    /// <summary>
    /// Creates a column index map by matching CSV headers with expected column mappings
    /// </summary>
    /// <param name="csvHeaders">Array of header names from CSV</param>
    /// <param name="columnMappings">Dictionary of expected column mappings</param>
    /// <returns>Dictionary mapping expected column names to CSV column indices</returns>
    private static Dictionary<string, int> CreateColumnIndexMap(
        string[] csvHeaders,
        Dictionary<string, string[]> columnMappings)
    {
        var columnIndexMap = new Dictionary<string, int>();
        var missingColumns = new List<string>();

        // Trim and normalize CSV headers for comparison
        var normalizedCsvHeaders = csvHeaders.Select((header, index) => new
        {
            OriginalIndex = index,
            NormalizedHeader = header.Trim().Replace("\"", "")
        }).ToArray();

        foreach (KeyValuePair<string, string[]> kvp in columnMappings)
        {
            string expectedColumn = kvp.Key;
            string[] possibleNames = kvp.Value;
            bool foundMatch = false;

            foreach (string possibleName in possibleNames)
            {
                var matchingHeader = normalizedCsvHeaders.FirstOrDefault(h =>
                    string.Equals(h.NormalizedHeader, possibleName, StringComparison.OrdinalIgnoreCase));

                if (matchingHeader is not null)
                {
                    columnIndexMap[expectedColumn] = matchingHeader.OriginalIndex;
                    foundMatch = true;
                    break;
                }
            }

            if (!foundMatch)
            {
                missingColumns.Add(expectedColumn);
            }
        }

        // Check if we have all required columns (allow missing DPD for regular LGD files)
        var criticalMissingColumns = missingColumns.Where(col => col != "DPD").ToList();

        if (criticalMissingColumns.Count > 0)
        {
            throw new InvalidOperationException(
                $"Missing required columns in CSV file: {string.Join(", ", criticalMissingColumns)}. " +
                $"Available columns: {string.Join(", ", normalizedCsvHeaders.Select(h => h.NormalizedHeader))}");
        }

        return columnIndexMap;
    }

    /// <summary>
    /// Parses a single CSV line into a LgdDetailsCreationRequest using column mapping
    /// </summary>
    /// <param name="line">CSV line to parse</param>
    /// <param name="lgdFileDetailsId">ID of the associated LgdFileDetails</param>
    /// <param name="columnIndexMap">Dictionary mapping column names to indices</param>
    /// <returns>LgdDetailsCreationRequest object</returns>
    private static LgdDetailsCreationRequest ParseCsvLine(
        string line,
        Guid lgdFileDetailsId,
        Dictionary<string, int> columnIndexMap)
    {
        string[] fields = ParseCsvFields(line);

        // Helper method to get field value by column name
        string GetFieldValue(string columnName) =>
            columnIndexMap.TryGetValue(columnName, out int index) && index < fields.Length
                ? GetStringValue(fields[index])
                : string.Empty;

        decimal GetFieldDecimal(string columnName) =>
            columnIndexMap.TryGetValue(columnName, out int index) && index < fields.Length
                ? ParseDecimal(fields[index])
                : 0m;

        int GetFieldInt(string columnName) =>
            columnIndexMap.TryGetValue(columnName, out int index) && index < fields.Length
                ? ParseInt(fields[index])
                : 0;

        DateTime GetFieldDateTime(string columnName) =>
            columnIndexMap.TryGetValue(columnName, out int index) && index < fields.Length
                ? ParseDateTime(fields[index])
                : DateTime.MinValue;

        DateTime? GetFieldNullableDateTime(string columnName) =>
            columnIndexMap.TryGetValue(columnName, out int index) && index < fields.Length
                ? ParseNullableDateTime(fields[index])
                : null;

        bool GetFieldYesNo(string columnName) =>
            columnIndexMap.TryGetValue(columnName, out int index) && index < fields.Length
                && ParseYesNo(fields[index]);

        string facilityNumber = GetFieldValue("Facility number");
        decimal cashflow = GetFieldDecimal("Cashflow");
        decimal dcf = LgdDiscountFactorCalculator.CalculateDiscountFactor(
            facilityNumber,
            GetFieldDateTime("Receipt Date"),
            GetFieldNullableDateTime("First NPL Date"),
            GetFieldDecimal("Interest Rate")
        );

        return new LgdDetailsCreationRequest(
            LgdFileDetailsId: lgdFileDetailsId,
            CustomerNumber: GetFieldValue("Customer Number"),
            FacilityNumber: facilityNumber,
            Branch: GetFieldValue("Branch"),
            ProductCategory: GetFieldValue("Product category"),
            Segment: GetFieldValue("Segment"),
            Industry: GetFieldValue("Industry"),
            EarningType: GetFieldValue("Earning Type"),
            Nature: GetFieldValue("Nature"),
            GrantDate: GetFieldDateTime("Grant date"),
            MaturityDate: GetFieldDateTime("Maturity date/ Expiry Date"),
            InterestRate: GetFieldDecimal("Interest Rate"),
            InstallmentType: GetFieldValue("Installment Type (Monthly/ Quarterly/ Weekly/ Daily/ Annually/ Bullet)"),
            DaysPastDue: GetFieldInt("Days Past Due"),
            Limit: GetFieldDecimal("Limit"),
            TotalOS: GetFieldDecimal("Total OS"),
            UndisbursedAmount: GetFieldDecimal("Undisbursed Amount"),
            InterestInSuspense: GetFieldDecimal("Interest in Suspense"),
            CollateralType: GetFieldValue("Collateral Type"),
            CollateralValue: GetFieldDecimal("Collateral Value"),
            Rescheduled: GetFieldYesNo("Rescheduled (Yes/No)"),
            Restructured: GetFieldYesNo("Restructured (Yes/No)"),
            NoOfTimesRestructured: GetFieldInt("No. of Times Restructured"),
            UpgradedToDelinquencyBucket: GetFieldYesNo("Upgraded to delinquency bucket (Yes/No)"),
            IndividuallyImpaired: GetFieldYesNo("Individually Impaired (Yes/No)"),
            BucketingInIndividualAssessment: GetFieldValue("Bucketing in Individual Assessment"),
            Period: GetFieldValue("Period"),
            FirstNplDate: GetFieldNullableDateTime("First NPL Date"),
            TotalOutstandingAsAtFirstNplDate: GetFieldDecimal("Total Outstanding as at First NPL Date"),
            ReceiptDate: GetFieldDateTime("Receipt Date"),
            ClosureDate: GetFieldDateTime("Closure Date"),
            Cashflow: cashflow,
            Dcf: dcf,
            DiscountedCashflows: LgdDiscountedCashflowsCalculator.CalculateDiscountedCashflows(
                facilityNumber,
                cashflow,
                dcf
            )
        );
    }

    /// <summary>
    /// Parses CSV fields handling quoted values with commas
    /// </summary>
    /// <param name="line">CSV line to parse</param>
    /// <returns>Array of field values</returns>
    private static string[] ParseCsvFields(string line)
    {
        var fields = new List<string>();
        bool inQuotes = false;
        int fieldStart = 0;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                string field = line.Substring(fieldStart, i - fieldStart).Trim();
                if (field.StartsWith('"') && field.EndsWith('"') && field.Length > 1)
                {
                    field = field.Substring(1, field.Length - 2); // Remove surrounding quotes
                }
                fields.Add(field);
                fieldStart = i + 1;
            }
        }

        // Add the last field
        string lastField = line.Substring(fieldStart).Trim();
        if (lastField.StartsWith('"') && lastField.EndsWith('"') && lastField.Length > 1)
        {
            lastField = lastField.Substring(1, lastField.Length - 2);
        }
        fields.Add(lastField);

        return fields.ToArray();
    }

    /// <summary>
    /// Gets string value from CSV field
    /// </summary>
    /// <param name="field">CSV field value</param>
    /// <returns>String value</returns>
    private static string GetStringValue(string field)
    {
        return string.IsNullOrWhiteSpace(field) ? string.Empty : field.Trim();
    }

    /// <summary>
    /// Parses decimal value from CSV field
    /// </summary>
    /// <param name="field">CSV field value</param>
    /// <returns>Decimal value</returns>
    private static decimal ParseDecimal(string field)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            return 0m;
        }

        string cleanField = field.Trim();
        if (decimal.TryParse(cleanField, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal result))
        {
            return result;
        }

        throw new FormatException($"Cannot parse decimal value: '{field}'");
    }

    /// <summary>
    /// Parses integer value from CSV field
    /// </summary>
    /// <param name="field">CSV field value</param>
    /// <returns>Integer value</returns>
    private static int ParseInt(string field)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            return 0;
        }

        string cleanField = field.Trim();
        if (int.TryParse(cleanField, out int result))
        {
            return result;
        }

        throw new FormatException($"Cannot parse integer value: '{field}'");
    }

    /// <summary>
    /// Parses DateTime value from CSV field
    /// </summary>
    /// <param name="field">CSV field value</param>
    /// <returns>DateTime value</returns>
    private static DateTime ParseDateTime(string field)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            return DateTime.MinValue;
        }

        string cleanField = field.Trim();

        // Try multiple date formats
        string[] formats =
        {
            "yyyy-MM-dd",
            "MM/dd/yyyy",
            "dd/MM/yyyy",
            "yyyy/MM/dd",
            "yyyy-MM-dd HH:mm:ss",
            "MM/dd/yyyy HH:mm:ss"
        };

        foreach (string format in formats)
        {
            if (DateTime.TryParseExact(cleanField, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            {
                // Ensure UTC for database consistency
                return DateTime.SpecifyKind(result, DateTimeKind.Utc);
            }
        }

        // Try general parsing as fallback
        if (DateTime.TryParse(cleanField, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime generalResult))
        {
            return DateTime.SpecifyKind(generalResult, DateTimeKind.Utc);
        }

        throw new FormatException($"Cannot parse datetime value: '{field}'");
    }

    /// <summary>
    /// Parses nullable DateTime value from CSV field
    /// </summary>
    /// <param name="field">CSV field value</param>
    /// <returns>Nullable DateTime value</returns>
    private static DateTime? ParseNullableDateTime(string field)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            return null;
        }

        try
        {
            return ParseDateTime(field);
        }
        catch (FormatException)
        {
            return null; // Return null for invalid dates in nullable fields
        }
    }

    /// <summary>
    /// Parses Yes/No value to boolean
    /// </summary>
    /// <param name="field">CSV field value</param>
    /// <returns>Boolean value</returns>
    private static bool ParseYesNo(string field)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            return false;
        }

        string cleanField = field.Trim();
        return string.Equals(cleanField, "Yes", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(cleanField, "Y", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(cleanField, "True", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Parses a single CSV line into a VCLgdDetailsCreationRequest using column mapping
    /// </summary>
    /// <param name="line">CSV line to parse</param>
    /// <param name="vcLgdFileDetailsId">ID of the associated VCLgdFileDetails</param>
    /// <param name="columnIndexMap">Dictionary mapping column names to indices</param>
    /// <returns>VCLgdDetailsCreationRequest object</returns>
    private static VCLgdDetailsCreationRequest ParseVCLgdCsvLine(
        string line,
        Guid vcLgdFileDetailsId,
        Dictionary<string, int> columnIndexMap)
    {
        string[] fields = ParseCsvFields(line);

        // Helper method to get field value by column name
        string GetFieldValue(string columnName) =>
            columnIndexMap.TryGetValue(columnName, out int index) && index < fields.Length
                ? GetStringValue(fields[index])
                : string.Empty;

        decimal GetFieldDecimal(string columnName) =>
            columnIndexMap.TryGetValue(columnName, out int index) && index < fields.Length
                ? ParseDecimal(fields[index])
                : 0m;

        int GetFieldInt(string columnName) =>
            columnIndexMap.TryGetValue(columnName, out int index) && index < fields.Length
                ? ParseInt(fields[index])
                : 0;

        DateTime GetFieldDateTime(string columnName) =>
            columnIndexMap.TryGetValue(columnName, out int index) && index < fields.Length
                ? ParseDateTime(fields[index])
                : DateTime.MinValue;

        DateTime? GetFieldNullableDateTime(string columnName) =>
            columnIndexMap.TryGetValue(columnName, out int index) && index < fields.Length
                ? ParseNullableDateTime(fields[index])
                : null;

        bool GetFieldYesNo(string columnName) =>
            columnIndexMap.TryGetValue(columnName, out int index) && index < fields.Length
                && ParseYesNo(fields[index]);

        string facilityNumber = GetFieldValue("Facility number");
        decimal cashflow = GetFieldDecimal("Cashflow");
        decimal dcf = LgdDiscountFactorCalculator.CalculateDiscountFactor(
            facilityNumber,
            GetFieldDateTime("Receipt Date"),
            GetFieldNullableDateTime("First NPL Date"),
            GetFieldDecimal("Interest Rate")
        );

        return new VCLgdDetailsCreationRequest(
            VCLgdFileDetailsId: vcLgdFileDetailsId,
            CustomerNumber: GetFieldValue("Customer Number"),
            FacilityNumber: facilityNumber,
            Branch: GetFieldValue("Branch"),
            ProductCategory: GetFieldValue("Product category"),
            Segment: GetFieldValue("Segment"),
            Industry: GetFieldValue("Industry"),
            EarningType: GetFieldValue("Earning Type"),
            Nature: GetFieldValue("Nature"),
            GrantDate: GetFieldDateTime("Grant date"),
            MaturityDate: GetFieldDateTime("Maturity date/ Expiry Date"),
            InterestRate: GetFieldDecimal("Interest Rate"),
            InstallmentType: GetFieldValue("Installment Type (Monthly/ Quarterly/ Weekly/ Daily/ Annually/ Bullet)"),
            DaysPastDue: GetFieldInt("Days Past Due"),
            DPD: GetFieldInt("DPD"), // VC LGD specific column
            Limit: GetFieldDecimal("Limit"),
            TotalOS: GetFieldDecimal("Total OS"),
            UndisbursedAmount: GetFieldDecimal("Undisbursed Amount"),
            InterestInSuspense: GetFieldDecimal("Interest in Suspense"),
            CollateralType: GetFieldValue("Collateral Type"),
            CollateralValue: GetFieldDecimal("Collateral Value"),
            Rescheduled: GetFieldYesNo("Rescheduled (Yes/No)"),
            Restructured: GetFieldYesNo("Restructured (Yes/No)"),
            NoOfTimesRestructured: GetFieldInt("No. of Times Restructured"),
            UpgradedToDelinquencyBucket: GetFieldYesNo("Upgraded to delinquency bucket (Yes/No)"),
            IndividuallyImpaired: GetFieldYesNo("Individually Impaired (Yes/No)"),
            BucketingInIndividualAssessment: GetFieldValue("Bucketing in Individual Assessment"),
            Period: GetFieldValue("Period"),
            FirstNplDate: GetFieldNullableDateTime("First NPL Date"),
            TotalOutstandingAsAtFirstNplDate: GetFieldDecimal("Total Outstanding as at First NPL Date"),
            ReceiptDate: GetFieldDateTime("Receipt Date"),
            ClosureDate: GetFieldDateTime("Closure Date"),
            Cashflow: cashflow,
            Dcf: dcf,
            DiscountedCashflows: cashflow * dcf
        );
    }

    /// <summary>
    /// Checks if a CSV line contains only empty or whitespace fields
    /// </summary>
    /// <param name="line">The CSV line to check</param>
    /// <returns>True if the line contains no meaningful data</returns>
    private static bool IsEmptyDataLine(string line)
    {
        // Quick check for lines with only commas
        if (line.Replace(",", "").Replace(" ", "").Replace("\t", "").Length == 0)
        {
            return true;
        }

        // Parse the fields and check if all are empty
        string[] fields = ParseCsvFields(line);

        // Check if all fields are empty or whitespace
        foreach (string field in fields)
        {
            if (!string.IsNullOrWhiteSpace(field))
            {
                return false; // Found non-empty field
            }
        }

        return true; // All fields are empty
    }
}