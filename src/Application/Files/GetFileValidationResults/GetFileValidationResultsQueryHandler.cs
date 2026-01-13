using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Files.Common;
using Application.Files.UploadFile;
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
            logger.LogWarning("GetFileValidationResultsQueryHandler missing CollectiveImpairmentType");
            return new PaginatedResult<FileValidationResultResponse>([], 0);
        }

        if (string.IsNullOrWhiteSpace(request.TimePeriod))
        {
            logger.LogWarning("GetFileValidationResultsQueryHandler missing TimePeriod");
            return new PaginatedResult<FileValidationResultResponse>([], 0);
        }

        if (!Enum.TryParse<ParameterType>(request.CollectiveImpairmentType, true, out ParameterType parameterType))
        {
            logger.LogWarning("GetFileValidationResultsQueryHandler invalid CollectiveImpairmentType: {CollectiveImpairmentType}",
                request.CollectiveImpairmentType);
            return new PaginatedResult<FileValidationResultResponse>([], 0);
        }

        CollectiveImpairmentConfig? config = await dbContext.CollectiveImpairmentConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Parameter == parameterType, cancellationToken);

        if (config is null)
        {
            logger.LogWarning("GetFileValidationResultsQueryHandler no config found for ParameterType: {ParameterType}", parameterType);
            return new PaginatedResult<FileValidationResultResponse>([], 0);
        }

        logger.LogInformation("GetFileValidationResultsQueryHandler config found, validating time period: {TimePeriod}", request.TimePeriod);

        Result<string> timePeriodValidation = FileProcessingUtilities.ValidateTimePeriod(request.TimePeriod, config.ConfigJson);

        if (!timePeriodValidation.IsSuccess)
        {
            logger.LogWarning("GetFileValidationResultsQueryHandler invalid time period: {TimePeriod}, Error: {Error}",
                request.TimePeriod, timePeriodValidation.Error.Description);
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

        string parameterFolder = Path.Combine(rootPath, parameterType.ToString(), "pending");
        logger.LogInformation("GetFileValidationResultsQueryHandler parameter folder: {ParameterFolder}", parameterFolder);

        // Create hierarchical folder structure based on frequency
        string timePeriodFolder = FileProcessingUtilities.CreateTimePeriodFolderPath(parameterFolder, request.TimePeriod, config.ConfigJson);
        logger.LogInformation("GetFileValidationResultsQueryHandler time period folder: {TimePeriodFolder}", timePeriodFolder);

        if (!Directory.Exists(timePeriodFolder))
        {
            logger.LogWarning("GetFileValidationResultsQueryHandler directory does not exist: {TimePeriodFolder}", timePeriodFolder);
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
            
            logger.LogInformation(
                "GetFileValidationResultsQueryHandler filtering by TimePeriod: {TimePeriod}, CollectiveImpairmentType: {Type}",
                request.TimePeriod, parameterType);

            if (!string.IsNullOrWhiteSpace(destinationPath))
            {
                if (!Directory.Exists(destinationPath))
                {
                    logger.LogInformation("GetFileValidationResultsQueryHandler creating destination directory: {DestinationPath}", destinationPath);
                    Directory.CreateDirectory(destinationPath);
                }

                files.AddRange(Directory.GetFiles(destinationPath));
            }
        }

        if (files.Count == 0)
        {
            logger.LogWarning("GetFileValidationResultsQueryHandler no files found in directory: {TimePeriodFolder}", timePeriodFolder);
            return new PaginatedResult<FileValidationResultResponse>([], 0);
        }

        // Build query with database-level filtering by TimePeriod and CollectiveImpairmentType
        var validationResultsQuery = from fvr in dbContext.FileValidationResults
                                     join uf in dbContext.UploadedFiles on fvr.Filename equals uf.StoredFileName into uploadedFileGroup
                                     from uf in uploadedFileGroup.DefaultIfEmpty()
                                     where fvr.TimePeriod == request.TimePeriod
                                        && fvr.CollectiveImpairmentType == parameterType.ToString()
                                     select new { ValidationResult = fvr, UploadedFile = uf };

        // Filter validation results to only include files from the directory
        string[] fileNames = [.. files.Select(Path.GetFileName).Where(f => f is not null).Cast<string>().Select(f => f.ToUpperInvariant())];

        logger.LogInformation("GetFileValidationResultsQueryHandler executing database query with TimePeriod and Type filters");

        // Get filtered results from database
        var allResults = await validationResultsQuery
            .Where(x => x.UploadedFile != null)
            .ToListAsync(cancellationToken);

        var filteredResults = allResults
            .Where(x => fileNames.Contains(x.UploadedFile!.StoredFileName.ToUpperInvariant()) || fileNames.Contains(x.UploadedFile!.OriginalFileName.ToUpperInvariant()))
            .OrderBy(x => x.ValidationResult.Id)
            .ToList(); // Materialize to avoid multiple enumeration

        logger.LogInformation(
            "GetFileValidationResultsQueryHandler retrieved {Count} validation results for TimePeriod: {TimePeriod}, Type: {Type}",
            filteredResults.Count, request.TimePeriod, parameterType);

        // Apply status filtering
        if (!string.IsNullOrWhiteSpace(request.Status) && request.Status != "all")
        {
            int beforeStatusFilter = filteredResults.Count;
            filteredResults = request.Status.ToUpperInvariant() switch
            {
                "VALID" => filteredResults.Where(x => x.ValidationResult.Status == "Valid").OrderBy(x => x.ValidationResult.Id).ToList(),
                "INVALID" => filteredResults.Where(x => x.ValidationResult.Status == "Invalid" ||
                                                        x.ValidationResult.Status == "HasValidationErrors").OrderBy(x => x.ValidationResult.Id).ToList(),
                _ => filteredResults
            };
            logger.LogInformation(
                "GetFileValidationResultsQueryHandler after status filter '{Status}': {FilteredCount} results (was {BeforeCount})",
                request.Status, filteredResults.Count, beforeStatusFilter);
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
                x.UploadedFile?.PhysicalPath,
                x.UploadedFile?.Id))
            .ToList();

        logger.LogInformation(
            "GetFileValidationResultsQueryHandler returning {ResultCount} results out of {TotalCount} for page {PageNumber}, " +
            "TimePeriod: {TimePeriod}, Type: {Type}",
            validationResults.Count, totalCount, request.PageNumber, request.TimePeriod, parameterType);

        return new PaginatedResult<FileValidationResultResponse>(validationResults, totalCount);
    }
}
