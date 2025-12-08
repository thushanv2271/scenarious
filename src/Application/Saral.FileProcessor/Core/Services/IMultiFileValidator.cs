namespace Saral.FileProcessor.Core.Services;

public interface IMultiFileValidator
{
    Task<CrossFileValidation> ValidateAcrossFilesAsync(
        IndividualFileResult[] individualResults,
        CancellationToken cancellationToken = default);
}