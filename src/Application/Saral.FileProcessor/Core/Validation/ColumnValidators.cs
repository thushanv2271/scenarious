namespace Saral.FileProcessor.Core.Validation;

public class NotEmptyValidator : IColumnValidator
{
    public string ColumnName { get; }

    public NotEmptyValidator(string columnName)
    {
        ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
    }

    public ValidationResult Validate(object? value, int rowIndex)
    {
        if (value is null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return new ValidationResult(false, $"Column '{ColumnName}' cannot be empty");
        }

        return new ValidationResult(true);
    }
}

public class NumericRangeValidator : IColumnValidator
{
    public string ColumnName { get; }
    public double? MinValue { get; }
    public double? MaxValue { get; }

    public NumericRangeValidator(string columnName, double? minValue = null, double? maxValue = null)
    {
        ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
        MinValue = minValue;
        MaxValue = maxValue;
    }

    public ValidationResult Validate(object? value, int rowIndex)
    {
        if (value is null)
        {
            return new ValidationResult(true); // Allow null values unless specifically validated otherwise
        }

        if (!double.TryParse(value.ToString(), out double numericValue))
        {
            return new ValidationResult(false, $"Column '{ColumnName}' must be a valid number");
        }

        if (MinValue.HasValue && numericValue < MinValue.Value)
        {
            return new ValidationResult(false, $"Column '{ColumnName}' must be >= {MinValue.Value}");
        }

        if (MaxValue.HasValue && numericValue > MaxValue.Value)
        {
            return new ValidationResult(false, $"Column '{ColumnName}' must be <= {MaxValue.Value}");
        }

        return new ValidationResult(true);
    }
}

public class DateValidator : IColumnValidator
{
    public string ColumnName { get; }

    public DateValidator(string columnName)
    {
        ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
    }

    public ValidationResult Validate(object? value, int rowIndex)
    {
        if (value is null)
        {
            return new ValidationResult(true); // Allow null values unless specifically validated otherwise
        }

        if (!DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            return new ValidationResult(false, $"Column '{ColumnName}' must be a valid date");
        }

        return new ValidationResult(true);
    }
}

public class UniqueValidator : IColumnValidator
{
    public string ColumnName { get; }
    private readonly HashSet<string> _seenValues;
    private readonly Dictionary<string, List<int>> _duplicateRows;

    public UniqueValidator(string columnName)
    {
        ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
        _seenValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _duplicateRows = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
    }

    public ValidationResult Validate(object? value, int rowIndex)
    {
        string stringValue = value?.ToString()?.Trim() ?? string.Empty;
        
        if (string.IsNullOrEmpty(stringValue))
        {
            return new ValidationResult(true); // Let NotEmptyValidator handle empty values
        }

        if (_seenValues.Contains(stringValue))
        {
            // Track all rows with this duplicate value
            if (!_duplicateRows.TryGetValue(stringValue, out List<int>? value1))
            {
                value1 = new List<int>();
                _duplicateRows[stringValue] = value1;
            }

            value1.Add(rowIndex);
            
            return new ValidationResult(false, $"Column '{ColumnName}' must be unique. Value '{stringValue}' appears in multiple rows");
        }

        _seenValues.Add(stringValue);
        return new ValidationResult(true);
    }

    public void Reset()
    {
        _seenValues.Clear();
        _duplicateRows.Clear();
    }
}

public class WarningValidator : IColumnValidator
{
    public string ColumnName { get; }
    private readonly string _warningMessage;

    public WarningValidator(string columnName, string warningMessage)
    {
        ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
        _warningMessage = warningMessage ?? "Warning message not specified";
    }

    public ValidationResult Validate(object? value, int rowIndex)
    {
        // Always returns true (valid) but with a warning message
        return new ValidationResult(true, _warningMessage);
    }
}

public class BooleanValidator : IColumnValidator
{
    public string ColumnName { get; }

    public BooleanValidator(string columnName)
    {
        ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
    }

    public ValidationResult Validate(object? value, int rowIndex)
    {
        if (value is null)
        {
            return new ValidationResult(true); // Allow null values unless specifically validated otherwise
        }

        string stringValue = value.ToString()?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(stringValue))
        {
            return new ValidationResult(true); // Let NotEmptyValidator handle empty values
        }

        if (bool.TryParse(stringValue, out _) || 
            stringValue.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
            stringValue.Equals("No", StringComparison.OrdinalIgnoreCase) ||
            stringValue.Equals("True", StringComparison.OrdinalIgnoreCase) ||
            stringValue.Equals("False", StringComparison.OrdinalIgnoreCase))
        {
            return new ValidationResult(true);
        }

        return new ValidationResult(false, $"Column '{ColumnName}' must be a valid boolean value (Yes/No, True/False)");
    }
}

public class ConditionalNotEmptyValidator : IColumnValidator
{
    public string ColumnName { get; }
    //private readonly string _conditionColumn;
    //private readonly string _conditionValue;
    //private readonly bool _allowEmptyWhenConditionMet;

    public ConditionalNotEmptyValidator(string columnName, string conditionColumn, string conditionValue, bool allowEmptyWhenConditionMet = true)
    {
        ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
        //_conditionColumn = conditionColumn ?? throw new ArgumentNullException(nameof(conditionColumn));
        //_conditionValue = conditionValue ?? throw new ArgumentNullException(nameof(conditionValue));
        //_allowEmptyWhenConditionMet = allowEmptyWhenConditionMet;
    }

    public ValidationResult Validate(object? value, int rowIndex)
    {
        // This validator needs access to the entire row data to check conditions
        // For now, we'll implement basic not empty validation
        // The conditional logic should be implemented at a higher level
        if (value is null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return new ValidationResult(false, $"Column '{ColumnName}' cannot be empty");
        }

        return new ValidationResult(true);
    }
}

public class DependentValidator : IColumnValidator
{
    public string ColumnName { get; }
    //private readonly string _dependentColumn;
    //private readonly string _requiredValue;

    public DependentValidator(string columnName, string dependentColumn, string requiredValue)
    {
        ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
        //_dependentColumn = dependentColumn ?? throw new ArgumentNullException(nameof(dependentColumn));
        //_requiredValue = requiredValue ?? throw new ArgumentNullException(nameof(requiredValue));
    }

    public ValidationResult Validate(object? value, int rowIndex)
    {
        // This validator needs access to the entire row data to check dependencies
        // For now, we'll implement basic not empty validation
        // The dependency logic should be implemented at a higher level
        if (value is null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return new ValidationResult(false, $"Column '{ColumnName}' cannot be empty");
        }

        return new ValidationResult(true);
    }
}
