namespace Saral.FileProcessor.Core.Services;

public interface IValidationConfigurationService
{
    IDataValidator ConfigureValidation(ReadOnlySpan<string> columnNames);
}