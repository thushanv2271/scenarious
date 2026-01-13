namespace Application.Files.GetFileValidationResults;

public sealed record FileValidationResultResponse(
    int Id,
    string Filename,
    int TotalRows,
    int TotalErrors,
    string Status,
    string? PhysicalPath,
    Guid? UploadedFileId
);