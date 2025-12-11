using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Files.Common;
using Application.Files.ProcessMultipleFiles;
using Application.Files.Services;
using Application.Files.UploadFile;
using DocumentFormat.OpenXml.InkML;
using Domain.CollectiveImpairment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Application.Files.GetFileValidationResults;

internal sealed class GetFileValidationResultsQueryHandler(
    IApplicationDbContext dbContext,
    IOptions<FileStorageOptions> storageOptions,
    IOptions<ProcessedFilePathsOptions> processedFilePathsOptions,
    ILogger<GetFileValidationResultsQueryHandler> logger)
    : IQueryHandler<GetFileValidationResultsQuery, PaginatedResult<FileValidationResultResponse>>
{
    public async Task<Result<PaginatedResult<FileValidationResultResponse>>> Handle(
        GetFileValidationResultsQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CollectiveImpairmentType))
        {
            //return Result.Failure<GetFileValidationResultsQueryHandlerResponse>(Error.Problem(
            //        "CollectiveImpairmentType.Required",
            //        "Collective impairment type is required."));
            return new PaginatedResult<FileValidationResultResponse>([], 0);
        }

        if (string.IsNullOrWhiteSpace(request.TimePeriod))
        {
            logger.LogWarning("GetFileValidationResultsQueryHandler missing TimePeriod.");
            return new PaginatedResult<FileValidationResultResponse>([], 0);
        }

        if (!Enum.TryParse<ParameterType>(request.CollectiveImpairmentType, true, out ParameterType parameterType))
        {
            logger.LogWarning("GetFileValidationResultsQueryHandler invalid CollectiveImpairmentType: {CollectiveImpairmentType}", request.CollectiveImpairmentType);
            //return Result.Failure<GetFileValidationResultsQueryHandlerResponse>(Error.Problem(
            //    "CollectiveImpairmentType.Invalid",
            //    $"Invalid collective impairment type. Valid values are: {string.Join(", ", Enum.GetNames<ParameterType>())}."));
            return new PaginatedResult<FileValidationResultResponse>([], 0);
        }

        CollectiveImpairmentConfig? config = await dbContext.CollectiveImpairmentConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Parameter == parameterType, cancellationToken);

        if (config is null)
        {
            logger.LogWarning("GetFileValidationResultsQueryHandler no config found for ParameterType: {ParameterType}", parameterType);
            //return Result.Failure<GetFileValidationResultsQueryHandlerResponse>(Error.Problem(
            //    "CollectiveImpairmentConfig.NotFound",
            //    $"No configuration found for collective impairment type '{parameterType}'."));
            return new PaginatedResult<FileValidationResultResponse>([], 0);
        }

        logger.LogInformation("GetFileValidationResultsQueryHandler config found, validating time period: {TimePeriod}", request.TimePeriod);

        Result<string> timePeriodValidation = FileProcessingUtilities.ValidateTimePeriod(request.TimePeriod, config.ConfigJson);

        if (!timePeriodValidation.IsSuccess)
        {
            return new PaginatedResult<FileValidationResultResponse>([], 0);
        }

        logger.LogInformation("GetFileValidationResultsQueryHandler building directory path from storage options");
        string configuredRoot = storageOptions.Value.RootPath ?? string.Empty;
        logger.LogInformation("GetFileValidationResultsQueryHandler configured root: {ConfiguredRoot}", configuredRoot);

        string expandedRoot = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.GetTempPath()
            : Environment.ExpandEnvironmentVariables(configuredRoot);

        string rootPath = Path.IsPathRooted(expandedRoot)
            ? expandedRoot
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, expandedRoot));

        string parameterFolder = Path.Combine(rootPath, parameterType.ToString());
        logger.LogInformation("GetFileValidationResultsQueryHandler parameter folder: {ParameterFolder}", parameterFolder);

        // Create hierarchical folder structure based on frequency
        string timePeriodFolder = FileProcessingUtilities.CreateTimePeriodFolderPath(parameterFolder, request.TimePeriod, config.ConfigJson);
        logger.LogInformation("GetFileValidationResultsQueryHandler time period folder: {TimePeriodFolder}", timePeriodFolder);

        if (!Directory.Exists(timePeriodFolder))
        {
            logger.LogWarning("GetFileValidationResultsQueryHandler directory does not exist: {TimePeriodFolder}", timePeriodFolder);
            //return Result.Failure<GetFileValidationResultsQueryHandlerResponse>(Error.Problem(
            //    "Directory.NotFound",
            //    $"No directory found '{timePeriodFolder}'."));
            return new PaginatedResult<FileValidationResultResponse>([], 0);
        }

        logger.LogInformation("GetFileValidationResultsQueryHandler scanning for files in directory: {TimePeriodFolder}", timePeriodFolder);
        List<string> files = [.. Directory.GetFiles(timePeriodFolder)];
        logger.LogInformation("GetFileValidationResultsQueryHandler found {FileCount} files", files.Count);

        if (processedFilePathsOptions.Value != null)
        {
            string? destinationPath = parameterType switch
            {
                ParameterType.PD => processedFilePathsOptions.Value.PD,
                ParameterType.LGD => processedFilePathsOptions.Value.LGD,
                _ => null
            };

            if (!string.IsNullOrWhiteSpace(destinationPath))
            {
                files.AddRange(Directory.GetFiles(destinationPath));
            }
        }

        if (files.Count == 0)
        {
            logger.LogWarning("GetFileValidationResultsQueryHandler no files found in directory: {TimePeriodFolder}", timePeriodFolder);
            //return Result.Failure<GetFileValidationResultsQueryHandlerResponse>(Error.Problem(
            //    "Files.NotFound",
            //    $"No files found in directory '{timePeriodFolder}'."));
            return new PaginatedResult<FileValidationResultResponse>([], 0);
        }

        var validationResultsQuery = from fvr in dbContext.FileValidationResults
                                     join uf in dbContext.UploadedFiles on fvr.Filename equals uf.StoredFileName into uploadedFileGroup
                                     from uf in uploadedFileGroup.DefaultIfEmpty()
                                     select new { ValidationResult = fvr, UploadedFile = uf };

        // Filter validation results to only include files from the directory
        string[] fileNames = [.. files.Select(Path.GetFileName).Where(f => f is not null).Cast<string>().Select(f => f.ToUpperInvariant())];
        
        // Get all results and apply client-side filtering for case-insensitive filename comparison
        var allResults = await validationResultsQuery
            .Where(x => x.UploadedFile != null)
            .ToListAsync(cancellationToken);
            
        var filteredResults = allResults
            .Where(x => fileNames.Contains(x.UploadedFile!.OriginalFileName.ToUpperInvariant()))
            .OrderBy(x => x.ValidationResult.Id)
            .ToList(); // Materialize to avoid multiple enumeration

        // Apply status filtering
        if (!string.IsNullOrWhiteSpace(request.Status) && request.Status != "all")
        {
            filteredResults = request.Status.ToUpperInvariant() switch
            {
                "VALID" => filteredResults.Where(x => x.ValidationResult.Status == "Valid").OrderBy(x => x.ValidationResult.Id).ToList(),
                "INVALID" => filteredResults.Where(x => x.ValidationResult.Status == "Invalid").OrderBy(x => x.ValidationResult.Id).ToList(),
                _ => filteredResults
            };
        }

        int totalCount = filteredResults.Count;

        var validationResults = filteredResults
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new FileValidationResultResponse(
                x.ValidationResult.Id,
                x.ValidationResult.Filename,
                x.ValidationResult.TotalRows,
                x.ValidationResult.TotalErrors,
                x.ValidationResult.Status,
                x.UploadedFile?.PhysicalPath))
            .ToList();

        return new PaginatedResult<FileValidationResultResponse>(validationResults, totalCount);
    }
}
