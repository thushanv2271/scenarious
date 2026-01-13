using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Files.Common;
using Domain.CollectiveImpairment;
using Domain.Exports;
using Domain.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Application.Files.UploadFile;

/// <summary>
/// Handles the processing and storage of uploaded files, ensuring validation of file type, content, and proper storage
/// in the configured file system.
/// </summary>
/// <remarks>This handler validates the uploaded file's extension and content type, sanitizes the file name, and
/// saves the file to a configured storage location. It also persists metadata about the uploaded file and logs the
/// operation. Supported file types include Excel (.xlsx, .xls) and CSV files.</remarks>
/// <param name="dbContext"></param>
/// <param name="logger"></param>
/// <param name="storageOptions"></param>
internal sealed class UploadFileCommandHandler(
    IApplicationDbContext dbContext,
    ILogger<UploadFileCommandHandler> logger,
    IOptions<FileStorageOptions> storageOptions
) : ICommandHandler<UploadFileCommand, UploadFileResponse>
{
    private static readonly HashSet<string> AllowedExtensions = [".xlsx", ".xls", ".csv"];
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.ms-excel",
        "text/csv",
        "application/csv"
    ];

    public async Task<Result<UploadFileResponse>> Handle(UploadFileCommand command, CancellationToken cancellationToken)
    {
        if (command is null)
        {
            return Result.Failure<UploadFileResponse>(Error.NullValue);
        }

        if (command.Files is null || command.Files.Count == 0)
        {
            return Result.Failure<UploadFileResponse>(Error.Problem(
                "Files.Empty",
                "No files provided for upload."));
        }

        // Validate CollectiveImpairmentType and fetch configuration
        if (string.IsNullOrWhiteSpace(command.CollectiveImpairmentType))
        {
            return Result.Failure<UploadFileResponse>(Error.Problem(
                "CollectiveImpairmentType.Required",
                "Collective impairment type is required."));
        }

        if (!Enum.TryParse<ParameterType>(command.CollectiveImpairmentType, true, out ParameterType parameterType))
        {
            return Result.Failure<UploadFileResponse>(Error.Problem(
                "CollectiveImpairmentType.Invalid",
                $"Invalid collective impairment type. Valid values are: {string.Join(", ", Enum.GetNames<ParameterType>())}."));
        }

        // Fetch the collective impairment configuration
        CollectiveImpairmentConfig? config = await dbContext.CollectiveImpairmentConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Parameter == parameterType, cancellationToken);

        if (config is null)
        {
            return Result.Failure<UploadFileResponse>(Error.Problem(
                "CollectiveImpairmentConfig.NotFound",
                $"No configuration found for collective impairment type '{parameterType}'."));
        }

        // Validate time period against configuration
        Result<string> timePeriodValidation = FileProcessingUtilities.ValidateTimePeriod(command.TimePeriod, config.ConfigJson);
        if (!timePeriodValidation.IsSuccess)
        {
            return Result.Failure<UploadFileResponse>(timePeriodValidation.Error);
        }

        // Validate each file
        foreach (FileUploadData fileData in command.Files)
        {
            if (fileData.Content is null || fileData.Content.Length == 0)
            {
                return Result.Failure<UploadFileResponse>(Error.Problem(
                    "File.Empty",
                    $"File '{fileData.FileName}' is empty."));
            }

            string ext = Path.GetExtension(fileData.FileName).ToUpperInvariant();
            if (!AllowedExtensions.Select(x => x.ToUpperInvariant()).Contains(ext.ToUpperInvariant()))
            {
                return Result.Failure<UploadFileResponse>(Error.Problem(
                    "File.InvalidType",
                    $"File '{fileData.FileName}' has invalid type. Only .xlsx, .xls and .csv files are allowed."));
            }

            if (!string.IsNullOrWhiteSpace(fileData.ContentType) && !AllowedContentTypes.Contains(fileData.ContentType))
            {
                return Result.Failure<UploadFileResponse>(Error.Problem(
                    "File.InvalidContentType",
                    $"File '{fileData.FileName}' has invalid content type. Allowed types: Excel or CSV."));
            }
        }

        // Create organized folder structure: RootPath/ParameterType/TimePeriod/
        string configuredRoot = storageOptions.Value.RootPath ?? string.Empty;
        string expandedRoot = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.GetTempPath()
            : Environment.ExpandEnvironmentVariables(configuredRoot);

        string rootPath = Path.IsPathRooted(expandedRoot)
            ? expandedRoot
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, expandedRoot));

        string parameterFolder = Path.Combine(rootPath, parameterType.ToString(), "pending");

        // Create hierarchical folder structure based on frequency
        string timePeriodFolder = FileProcessingUtilities.CreateTimePeriodFolderPath(parameterFolder, command.TimePeriod, config.ConfigJson);

        if (!Directory.Exists(timePeriodFolder))
        {
            Directory.CreateDirectory(timePeriodFolder);
            logger.LogInformation("Created directory structure: '{DirectoryPath}'", timePeriodFolder);
        }

        // Process all files
        var uploadedFiles = new List<UploadedFileInfo>();
        long totalSize = 0;

        foreach (FileUploadData fileData in command.Files)
        {
            string sanitizedBase = FileProcessingUtilities.SanitizeFileNameWithoutExtension(Path.GetFileNameWithoutExtension(fileData.FileName));
            string baseWithUnderscores = FileProcessingUtilities.ReplaceWhitespaceWithUnderscore(sanitizedBase);
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string ext = Path.GetExtension(fileData.FileName).ToUpperInvariant();
            string guidPart = Guid.CreateVersion7().ToString("N")[..8];
            string storedFileName = $"{baseWithUnderscores}_{timestamp}_{guidPart}{ext}";
            string filePath = Path.Combine(timePeriodFolder, storedFileName);

            await File.WriteAllBytesAsync(filePath, fileData.Content, cancellationToken);
            logger.LogInformation("Physical file saved at '{PhysicalPath}'", filePath);

            string savedLocation = !string.IsNullOrWhiteSpace(storageOptions.Value.PublicBaseUrl)
                ? $"{storageOptions.Value.PublicBaseUrl!.TrimEnd('/')}/{Uri.EscapeDataString(storedFileName)}"
                : new Uri(filePath).AbsoluteUri;

            // Persist UploadedFile entity
            var uploaded = new UploadedFile
            {
                Id = Guid.NewGuid(),
                OriginalFileName = fileData.FileName,
                StoredFileName = storedFileName,
                ContentType = fileData.ContentType,
                Size = fileData.Content.LongLength,
                PhysicalPath = filePath,
                PublicUrl = savedLocation,
                UploadedBy = command.UploadedBy
            };

            await dbContext.UploadedFiles.AddAsync(uploaded, cancellationToken);

            uploadedFiles.Add(new UploadedFileInfo(
                Id: uploaded.Id,
                Url: new Uri(savedLocation),
                StoredFileName: storedFileName,
                OriginalFileName: fileData.FileName,
                Size: fileData.Content.LongLength
            ));

            totalSize += fileData.Content.LongLength;

            // Optional: keep existing audit for each file
            var audit = new ExportAudit(
                exportedBy: command.UploadedBy,
                file: savedLocation,
                category: "File Upload"
            );

            await dbContext.ExportAudits.AddAsync(audit, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Uploaded {FileCount} files by {UserId} to '{Location}' with total size {TotalSize} bytes",
            command.Files.Count, command.UploadedBy, timePeriodFolder, totalSize);

        var response = new UploadFileResponse(
            UploadedFiles: uploadedFiles,
            TotalFiles: command.Files.Count,
            TotalSize: totalSize
        );

        return Result.Success(response);
    }
}
