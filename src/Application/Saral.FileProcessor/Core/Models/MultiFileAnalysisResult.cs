namespace Saral.FileProcessor.Core.Models;

public sealed record MultiFileAnalysisResult
{
    public required IReadOnlyList<IndividualFileResult> IndividualResults { get; init; }
    public required ConsolidatedAnalysis ConsolidatedAnalysis { get; init; }
    public required CrossFileValidation CrossFileValidation { get; init; }
    public required MultiFileSummaryStatistics SummaryStatistics { get; init; }
    public DateTimeOffset ProcessedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record IndividualFileResult
{
    public required string FileName { get; init; }
    public required AnalysisResult Analysis { get; init; }
    public required int FileIndex { get; init; }
}

public sealed record ConsolidatedAnalysis
{
    public required int TotalRows { get; init; }
    public required int TotalColumns { get; init; }
    public required ValidationSummary ValidationSummary { get; init; }
    public required IReadOnlyDictionary<string, ColumnMetrics> ColumnMetrics { get; init; }
    public required IReadOnlyList<CrossFileValidationError> CrossFileErrors { get; init; }
}

public sealed record CrossFileValidation
{
    public required int TotalDuplicateRows { get; init; }
    public required IReadOnlyList<CrossFileDuplicate> Duplicates { get; init; }
    public required IReadOnlyDictionary<string, UniqueValueValidation> UniqueValueViolations { get; init; }
    
    // Additional validation types
    public required IReadOnlyList<CrossFileValidationError> CrossFileValidationErrors { get; init; }
    public required IReadOnlyList<ConditionalValidationError> ConditionalValidationErrors { get; init; }
    public required IReadOnlyList<DependentValidationError> DependentValidationErrors { get; init; }
    public required IReadOnlyList<NumericRangeValidationError> NumericRangeValidationErrors { get; init; }
    public required IReadOnlyList<DateValidationError> DateValidationErrors { get; init; }
    public required IReadOnlyList<BooleanValidationError> BooleanValidationErrors { get; init; }
}

public sealed record CrossFileDuplicate
{
    public required string RowSignature { get; init; }
    public required IReadOnlyList<FileRowLocation> Locations { get; init; }
    public required int OccurrenceCount { get; init; }
    public required string ErrorType { get; init; } = "Duplicate Row";
}

public readonly record struct FileRowLocation(string FileName, int RowIndex);

public sealed record UniqueValueValidation
{
    public required string ColumnName { get; init; }
    public required string Value { get; init; }
    public required IReadOnlyList<FileRowLocation> Locations { get; init; }
    public required string ViolationType { get; init; } = "Unique Constraint Violation";
}

public sealed record ConditionalValidationError
{
    public required string ErrorType { get; init; }
    public required string Message { get; init; }
    public required FileRowLocation Location { get; init; }
    public required string ConditionColumn { get; init; }
    public required string? ConditionValue { get; init; }
    public required string DependentColumn { get; init; }
    public required string? DependentValue { get; init; }
}

public sealed record DependentValidationError
{
    public required string ErrorType { get; init; }
    public required string Message { get; init; }
    public required FileRowLocation Location { get; init; }
    public required string PrimaryColumn { get; init; }
    public required string? PrimaryValue { get; init; }
    public required string DependentColumn { get; init; }
    public required string? DependentValue { get; init; }
}

public sealed record NumericRangeValidationError
{
    public required string ErrorType { get; init; }
    public required string Message { get; init; }
    public required FileRowLocation Location { get; init; }
    public required string ColumnName { get; init; }
    public required string Value { get; init; }
    public required double? MinValue { get; init; }
    public required double? MaxValue { get; init; }
}

public sealed record DateValidationError
{
    public required string ErrorType { get; init; }
    public required string Message { get; init; }
    public required FileRowLocation Location { get; init; }
    public required string ColumnName { get; init; }
    public required string Value { get; init; }
    public required DateTime? MinDate { get; init; }
    public required DateTime? MaxDate { get; init; }
}

public sealed record BooleanValidationError
{
    public required string ErrorType { get; init; }
    public required string Message { get; init; }
    public required FileRowLocation Location { get; init; }
    public required string ColumnName { get; init; }
    public required string Value { get; init; }
    public required IReadOnlyList<string> AcceptedValues { get; init; }
}

public sealed record CrossFileValidationError
{
    public required string ErrorType { get; init; }
    public required string Message { get; init; }
    public required IReadOnlyList<FileRowLocation> Locations { get; init; }
    public required string ValidationRule { get; init; }
    public required string AffectedColumn { get; init; }
}

public sealed record MultiFileSummaryStatistics
{
    public required int TotalFiles { get; init; }
    public required int TotalRows { get; init; }
    public required int ValidRows { get; init; }
    public required int InvalidRows { get; init; }
    public required int CrossFileValidationErrors { get; init; }
    public required double DataQualityScore { get; init; }
    public required TimeSpan ProcessingTime { get; init; }
}