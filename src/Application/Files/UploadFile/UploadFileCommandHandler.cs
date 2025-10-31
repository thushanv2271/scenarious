using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Exports;
using Domain.Files;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;
using System;
using System.Globalization;
using System.IO;
using System.Text;

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

        if (command.Content is null || command.Content.Length == 0)
        {
            return Result.Failure<UploadFileResponse>(Error.Problem(
                "File.Empty",
                "The uploaded file is empty."));
        }

        string ext = Path.GetExtension(command.FileName).ToUpperInvariant();
        if (!AllowedExtensions.Select(x => x.ToUpperInvariant()).Contains(ext.ToUpperInvariant()))
        {
            return Result.Failure<UploadFileResponse>(Error.Problem(
                "File.InvalidType",
                "Only .xlsx, .xls and .csv files are allowed."));
        }

        if (!string.IsNullOrWhiteSpace(command.ContentType) && !AllowedContentTypes.Contains(command.ContentType))
        {
            // Soft check: do not reject if content-type is missing; reject only when present and invalid
            return Result.Failure<UploadFileResponse>(Error.Problem(
                "File.InvalidContentType",
                "Invalid content type. Allowed types: Excel or CSV."));
        }

        string sanitizedBase = SanitizeFileNameWithoutExtension(Path.GetFileNameWithoutExtension(command.FileName));
        string baseWithUnderscores = ReplaceWhitespaceWithUnderscore(sanitizedBase);
        string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        string storedFileName = $"{baseWithUnderscores}_{timestamp}{ext}";

        // Save physically to server using configured root path
        string configuredRoot = storageOptions.Value.RootPath ?? string.Empty;
        string expandedRoot = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.GetTempPath()
            : Environment.ExpandEnvironmentVariables(configuredRoot);

        // If relative, make absolute under the app base directory
        string rootPath = Path.IsPathRooted(expandedRoot)
            ? expandedRoot
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, expandedRoot));

        if (!Directory.Exists(rootPath))
        {
            Directory.CreateDirectory(rootPath);
        }

        string filePath = Path.Combine(rootPath, storedFileName);

        await File.WriteAllBytesAsync(filePath, command.Content, cancellationToken);
        logger.LogInformation("Physical file saved at '{PhysicalPath}'", filePath);

        string savedLocation = !string.IsNullOrWhiteSpace(storageOptions.Value.PublicBaseUrl)
            ? $"{storageOptions.Value.PublicBaseUrl!.TrimEnd('/')}/{Uri.EscapeDataString(storedFileName)}"
            : new Uri(filePath).AbsoluteUri;

        // Persist UploadedFile entity
        var uploaded = new UploadedFile
        {
            Id = Guid.NewGuid(),
            OriginalFileName = command.FileName,
            StoredFileName = storedFileName,
            ContentType = command.ContentType,
            Size = command.Content.LongLength,
            PhysicalPath = filePath,
            PublicUrl = savedLocation,
            UploadedBy = command.UploadedBy
        };

        await dbContext.UploadedFiles.AddAsync(uploaded, cancellationToken);

        // Optional: keep existing audit
        var audit = new ExportAudit(
            exportedBy: command.UploadedBy,
            file: savedLocation,
            category: "File Upload"
        );

        await dbContext.ExportAudits.AddAsync(audit, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("File '{FileName}' uploaded by {UserId} to '{Location}'", storedFileName, command.UploadedBy, savedLocation);

        var response = new UploadFileResponse(
            Id: uploaded.Id,
            Url: new Uri(savedLocation),
            StoredFileName: storedFileName,
            Size: command.Content.LongLength
        );

        return Result.Success(response);
    }

    private static string SanitizeFileNameWithoutExtension(string name)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        char[] sanitized = new char[name.Length];
        int idx = 0;

        foreach (char c in name)
        {
            sanitized[idx++] = invalid.Contains(c) ? '_' : c;
        }

        string result = new string(sanitized, 0, idx).Trim();
        return string.IsNullOrWhiteSpace(result) ? "upload" : result;
    }

    private static string ReplaceWhitespaceWithUnderscore(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var sb = new StringBuilder(input.Length);
        bool lastWasUnderscore = false;

        foreach (char ch in input)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!lastWasUnderscore)
                {
                    sb.Append('_');
                    lastWasUnderscore = true;
                }
            }
            else
            {
                sb.Append(ch);
                lastWasUnderscore = false;
            }
        }

        return sb.ToString();
    }
}
