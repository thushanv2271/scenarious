namespace Saral.FileProcessor.Core.Abstractions;

public interface IColumnValidator
{
    string ColumnName { get; }
    ValidationResult Validate(object? value, int rowIndex);
}

public interface IDataValidator
{
    ValidationSummary ValidateData(FileLoadContext loadContext);
}