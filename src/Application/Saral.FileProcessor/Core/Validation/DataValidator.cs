namespace Saral.FileProcessor.Core.Validation;

public class DataValidator : IDataValidator
{
    private readonly List<IColumnValidator> _validators;
    private const string ValidationColumnName = "Validation_Messages";

    public DataValidator()
    {
        _validators = new List<IColumnValidator>();
    }

    public DataValidator AddValidator(IColumnValidator validator)
    {
        _validators.Add(validator);
        return this;
    }

    public DataValidator AddValidators(params IColumnValidator[] validators)
    {
        _validators.AddRange(validators);
        return this;
    }

    public ValidationSummary ValidateData(FileLoadContext loadContext)
    {
        ArgumentNullException.ThrowIfNull(loadContext);

        Frame<int, string> frame = loadContext.Data;
        var rowValidations = new List<RowValidation>();

        // Reset any stateful validators (like UniqueValidator)
        foreach (UniqueValidator validator in _validators.OfType<UniqueValidator>())
        {
            validator.Reset();
        }

        // First pass: Validate each row
        foreach (int rowKey in frame.RowKeys.OrderBy(x => x))
        {
            IReadOnlyDictionary<string, object?> rowValues = FrameUtilities.CaptureRowValues(frame, rowKey);
            var columnValidations = new Dictionary<string, ValidationResult>();

            // Apply validators for this row
            foreach (IColumnValidator validator in _validators)
            {
                if (rowValues.ContainsKey(validator.ColumnName))
                {
                    object? value = rowValues[validator.ColumnName];
                    ValidationResult result = validator.Validate(value, rowKey);
                    columnValidations[validator.ColumnName] = result;
                }
            }

            rowValidations.Add(new RowValidation(rowKey, columnValidations));
        }

        // Second pass for unique validators: Mark all duplicates
        var uniqueValidators = _validators.OfType<UniqueValidator>().ToList();
        if (uniqueValidators.Any())
        {
            foreach (UniqueValidator? uniqueValidator in uniqueValidators)
            {
                string columnName = uniqueValidator.ColumnName;
                var valueGroups = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
                
                // Group rows by column value
                foreach (int rowKey in frame.RowKeys.OrderBy(x => x))
                {
                    IReadOnlyDictionary<string, object?> rowValues = FrameUtilities.CaptureRowValues(frame, rowKey);
                    if (rowValues.ContainsKey(columnName))
                    {
                        string value = rowValues[columnName]?.ToString()?.Trim() ?? string.Empty;
                        if (!string.IsNullOrEmpty(value))
                        {
                            if (!valueGroups.TryGetValue(value, out List<int>? value1))
                            {
                                value1 = [];
                                valueGroups[value] = value1;
                            }

                            value1.Add(rowKey);
                        }
                    }
                }

                // Mark duplicates
                foreach (KeyValuePair<string, List<int>> group in valueGroups.Where(g => g.Value.Count > 1))
                {
                    foreach (int rowKey in group.Value)
                    {
                        RowValidation? rowValidation = rowValidations.FirstOrDefault(rv => rv.RowIndex == rowKey);
                        if (rowValidation != null)
                        {
                            var updatedValidations = new Dictionary<string, ValidationResult>(rowValidation.ColumnValidations)
                            {
                                [columnName] = new ValidationResult(false,
                                $"Column '{columnName}' must be unique. Value '{group.Key}' appears in multiple rows")
                            };

                            int index = rowValidations.FindIndex(rv => rv.RowIndex == rowKey);
                            rowValidations[index] = new RowValidation(rowKey, updatedValidations);
                        }
                    }
                }
            }
        }

        var validationArray = rowValidations.ToImmutableArray();

        // Always create a modified data frame with validation messages column
        Frame<int, string> modifiedData = CreateDataFrameWithValidationColumn(frame, validationArray);

        return new ValidationSummary(validationArray, modifiedData);
    }

    private static Frame<int, string> CreateDataFrameWithValidationColumn(
        Frame<int, string> originalFrame, 
        ImmutableArray<RowValidation> rowValidations)
    {
        // Get all original columns
        var originalColumns = originalFrame.ColumnKeys.ToList();

        // Check if validation column already exists and remove it if it does
        bool validationColumnExists = originalColumns.Contains(ValidationColumnName);
        if (validationColumnExists)
        {
            originalColumns.Remove(ValidationColumnName);
        }
        
        //var newColumns = new List<string>(originalColumns) { ValidationColumnName };

        // Create series list preserving the original row order
        var seriesList = new List<KeyValuePair<string, Series<int, object?>>>();
        
        // Add original columns maintaining row order (excluding existing validation column)
        foreach (string? column in originalColumns)
        {
            Series<int, object?> columnSeries = originalFrame.GetColumn<object?>(column);
            var builder = new SeriesBuilder<int, object?>();
            
            // Add values in the original row order
            foreach (int rowKey in originalFrame.RowKeys.OrderBy(x => x))
            {
                OptionalValue<object?> optional = columnSeries.TryGet(rowKey);
                builder.Add(rowKey, optional.HasValue ? optional.Value : null);
            }
            
            seriesList.Add(new KeyValuePair<string, Series<int, object?>>(column, builder.Series));
        }

        // Add validation messages column maintaining row order
        var validationBuilder = new SeriesBuilder<int, object?>();
        var validationDict = rowValidations.ToDictionary(rv => rv.RowIndex, rv => rv.ValidationMessage ?? string.Empty);
        
        foreach (int rowKey in originalFrame.RowKeys.OrderBy(x => x))
        {
            string message = validationDict.TryGetValue(rowKey, out string? value) ? value : string.Empty;
            validationBuilder.Add(rowKey, message);
        }
        
        seriesList.Add(new KeyValuePair<string, Series<int, object?>>(ValidationColumnName, validationBuilder.Series));

        return Frame.FromColumns(seriesList);
    }
}
