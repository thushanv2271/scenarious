using Deedle;
using Saral.FileProcessor.Core.Models;
using System.Collections.Immutable;

namespace Saral.FileProcessor.Core.Services;

/// <summary>
/// Validates multiple files by performing cross-file analysis such as finding duplicates across files
/// </summary>
public class MultiFileValidator : IMultiFileValidator
{
    /// <summary>
    /// Validates multiple files by performing cross-file analysis
    /// </summary>
    public async Task<CrossFileValidation> ValidateAcrossFilesAsync(
        IndividualFileResult[] individualResults,
        CancellationToken cancellationToken = default)
    {
        var duplicates = new List<CrossFileDuplicate>();
        var uniqueValueViolations = new Dictionary<string, UniqueValueValidation>();
        //var crossFileValidationErrors = new List<CrossFileValidationError>();

        // Find duplicates across all files
        await FindCrossFileDuplicatesAsync(individualResults, duplicates);

        // Check for unique value violations across files
        await FindUniqueValueViolationsAsync(individualResults, uniqueValueViolations);

        return new CrossFileValidation
        {
            TotalDuplicateRows = duplicates.Sum(d => d.OccurrenceCount),
            Duplicates = duplicates.AsReadOnly(),
            UniqueValueViolations = uniqueValueViolations.AsReadOnly(),
            CrossFileValidationErrors = uniqueValueViolations.Values.SelectMany(v =>
                v.Locations.Select(l => new CrossFileValidationError
                {
                    ErrorType = "Unique Constraint Violation",
                    Message = $"Column '{v.ColumnName}' must be unique across all files. Value '{v.Value}' appears in multiple files",
                    Locations = new[] { l }.ToList().AsReadOnly(),
                    ValidationRule = "Cross-file uniqueness",
                    AffectedColumn = v.ColumnName
                })).ToList().AsReadOnly(),
            ConditionalValidationErrors = Array.Empty<ConditionalValidationError>(),
            DependentValidationErrors = Array.Empty<DependentValidationError>(),
            NumericRangeValidationErrors = Array.Empty<NumericRangeValidationError>(),
            DateValidationErrors = Array.Empty<DateValidationError>(),
            BooleanValidationErrors = Array.Empty<BooleanValidationError>()
        };
    }

    private Task FindCrossFileDuplicatesAsync(
        IndividualFileResult[] individualResults,
        List<CrossFileDuplicate> duplicates)
    {
        var allRows = new List<(string fileName, int rowIndex, string rowSignature)>();

        // Extract all rows from all files
        foreach (IndividualFileResult fileResult in individualResults)
        {
            Frame<int, string> frame = fileResult.Analysis.FileContext.Data;
            string fileName = fileResult.FileName;

            // Get all row keys to iterate through
            int[] rowKeys = [.. frame.RowKeys];

            for (int i = 0; i < rowKeys.Length; i++)
            {
                int rowKey = rowKeys[i];
                Series<string, object> row = frame.GetRow<object>(rowKey);

                // Create a signature for the row by concatenating all values
                string[] values = [.. frame.ColumnKeys.Select(col => row.TryGet(col).HasValue ? row[col]?.ToString() ?? "" : "")];
                string signature = string.Join("|", values);

                allRows.Add((fileName, i, signature));
            }
        }

        // Group by signature to find duplicates
        var duplicateGroups = allRows
            .GroupBy(r => r.rowSignature)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (IGrouping<string, (string fileName, int rowIndex, string rowSignature)>? group in duplicateGroups)
        {
            var locations = group.Select(r => new FileRowLocation
            {
                FileName = r.fileName,
                RowIndex = r.rowIndex
            }).ToList();

            duplicates.Add(new CrossFileDuplicate
            {
                RowSignature = group.Key,
                Locations = locations.AsReadOnly(),
                OccurrenceCount = group.Count(),
                ErrorType = "Duplicate Row Across Files"
            });
        }

        return Task.CompletedTask;
    }

    private async Task FindUniqueValueViolationsAsync(
        IndividualFileResult[] individualResults,
        Dictionary<string, UniqueValueValidation> uniqueValueViolations)
    {
        // Check for Facility number duplicates across files
        var facilityNumberTracker = new Dictionary<string, List<(string fileName, int rowIndex)>>(StringComparer.OrdinalIgnoreCase);

        foreach (IndividualFileResult fileResult in individualResults)
        {
            Frame<int, string> frame = fileResult.Analysis.FileContext.Data;
            string fileName = fileResult.FileName;

            // Check if Facility number column exists
            if (!frame.ColumnKeys.Contains("Facility number"))
            {
                continue;
            }

            int[] rowKeys = frame.RowKeys.ToArray();

            for (int i = 0; i < rowKeys.Length; i++)
            {
                int rowKey = rowKeys[i];
                Series<string, object> row = frame.GetRow<object>(rowKey);

                if (row.TryGet("Facility number").HasValue)
                {
                    string? facilityNumber = row["Facility number"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(facilityNumber))
                    {
                        if (!facilityNumberTracker.TryGetValue(facilityNumber, out List<(string fileName, int rowIndex)>? value))
                        {
                            value = new List<(string, int)>();
                            facilityNumberTracker[facilityNumber] = value;
                        }

                        value.Add((fileName, i));
                    }
                }
            }
        }

        // Find duplicates
        foreach (KeyValuePair<string, List<(string fileName, int rowIndex)>> kvp in facilityNumberTracker.Where(x => x.Value.Count > 1))
        {
            string facilityNumber = kvp.Key;
            List<(string fileName, int rowIndex)> locations = kvp.Value;

            uniqueValueViolations[facilityNumber] = new UniqueValueValidation
            {
                ColumnName = "Facility number",
                Value = facilityNumber,
                Locations = locations.Select(l => new FileRowLocation(l.fileName, l.rowIndex)).ToList().AsReadOnly(),
                ViolationType = "Cross-file Unique Constraint Violation"
            };
        }

        await Task.CompletedTask;
    }
}
