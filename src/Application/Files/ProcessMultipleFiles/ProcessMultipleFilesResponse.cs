namespace Application.Files.ProcessMultipleFiles;

/// <summary>
/// Represents the response from processing multiple files.
/// </summary>
/// <param name="TotalFiles">The total number of files processed.</param>
/// <param name="TotalRows">The total number of rows across all files.</param>
/// <param name="CrossFileDuplicates">The number of duplicate rows found across files.</param>
/// <param name="CrossFileValidationErrors">The number of cross-file validation errors.</param>
/// <param name="ProcessingTimeMs">The processing time in milliseconds.</param>
/// <param name="DataQualityScore">The overall data quality score.</param>
/// <param name="IndividualResults">The results for each individual file.</param>
/// <param name="JsonReportPath">The path to the generated JSON report.</param>
/// <param name="ExcelReportPath">The path to the generated Excel report.</param>
/// <param name="FileMovementErrors">Any errors encountered during file movement operations.</param>
public sealed record ProcessMultipleFilesResponse(
    int TotalFiles,
    long TotalRows,
    int CrossFileDuplicates,
    int CrossFileValidationErrors,
    double ProcessingTimeMs,
    double DataQualityScore,
    IReadOnlyList<IndividualFileResult> IndividualResults,
    string? JsonReportPath,
    string? ExcelReportPath,
    string? FileMovementErrors
);

/// <summary>
/// Represents the result of processing an individual file.
/// </summary>
/// <param name="FileName">The name of the file.</param>
/// <param name="FileIndex">The index of the file in the processing batch.</param>
/// <param name="TotalRows">The total number of rows in the file.</param>
/// <param name="TotalColumns">The total number of columns in the file.</param>
/// <param name="ValidRows">The number of valid rows in the file.</param>
/// <param name="InvalidRows">The number of invalid rows in the file.</param>
/// <param name="DataQualityScore">The data quality score for this file.</param>
/// <param name="ValidationSuccessRate">The validation success rate as a percentage.</param>
/// <param name="ValidationErrors">Detailed validation errors for invalid rows.</param>
public sealed record IndividualFileResult(
    string FileName,
    int FileIndex,
    int TotalRows,
    int TotalColumns,
    int ValidRows,
    int InvalidRows,
    double DataQualityScore,
    double ValidationSuccessRate,
    IReadOnlyList<ValidationErrorDetail> ValidationErrors
);

/// <summary>
/// Represents a validation error for a specific row.
/// </summary>
/// <param name="RowIndex">The index of the row with the error.</param>
/// <param name="ErrorMessage">The validation error message.</param>
/// <param name="ColumnErrors">Specific column validation errors for this row.</param>
public sealed record ValidationErrorDetail(
    int RowIndex,
    string? ErrorMessage,
    IReadOnlyDictionary<string, string>? ColumnErrors
);