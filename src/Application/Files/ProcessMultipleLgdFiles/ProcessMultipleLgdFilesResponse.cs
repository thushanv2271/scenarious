namespace Application.Files.ProcessMultipleLgdFiles;

public sealed record ProcessMultipleLgdFilesResponse(
    int TotalFiles,
    int TotalRows,
    int CrossFileDuplicates,
    int CrossFileValidationErrors,
    double ProcessingTimeMs,
    double DataQualityScore,
    LgdIndividualFileResult[] IndividualResults,
    string? JsonReportPath,
    string? ExcelReportPath,
    string? FileMovementErrors
);

public sealed record LgdIndividualFileResult(
    string FileName,
    int FileIndex,
    int TotalRows,
    int TotalColumns,
    int ValidRows,
    int InvalidRows,
    double DataQualityScore,
    double ValidationSuccessRate,
    List<LgdValidationErrorDetail> ValidationErrors
);

public sealed record LgdValidationErrorDetail(
    int RowIndex,
    string ErrorMessage,
    Dictionary<string, string> ColumnErrors
);
