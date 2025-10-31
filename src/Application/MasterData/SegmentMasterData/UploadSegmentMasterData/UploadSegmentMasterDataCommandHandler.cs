using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.MasterData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using SharedKernel;

namespace Application.MasterData.SegmentMasterData.UploadSegmentMasterData;

/// <summary>
/// Handles the uploading of SegmentMasterData.xlsx file to the configured location and automatically refreshes the database.
/// </summary>
/// <remarks>This handler validates the uploaded file is an Excel file (.xlsx), 
/// ensures the filename matches the expected SegmentMasterData.xlsx pattern, 
/// saves the file to the configured SegmentMasterDataPath location, and then automatically
/// clears existing SegmentMaster data and loads the new data from the uploaded Excel file.</remarks>
internal sealed class UploadSegmentMasterDataCommandHandler(
    IConfiguration configuration,
    ILogger<UploadSegmentMasterDataCommandHandler> logger,
    IApplicationDbContext dbContext
) : ICommandHandler<UploadSegmentMasterDataCommand, UploadSegmentMasterDataResponse>
{
    private static readonly HashSet<string> AllowedExtensions = [".xlsx"];
    private const string ExpectedFileName = "SegmentMasterData.xlsx";

    public async Task<Result<UploadSegmentMasterDataResponse>> Handle(
        UploadSegmentMasterDataCommand command, 
        CancellationToken cancellationToken)
    {
        if (command is null)
        {
            return Result.Failure<UploadSegmentMasterDataResponse>(Error.NullValue);
        }

        if (command.Content is null || command.Content.Length == 0)
        {
            return Result.Failure<UploadSegmentMasterDataResponse>(Error.Problem(
                "File.Empty",
                "The uploaded file is empty."));
        }

        string ext = Path.GetExtension(command.FileName).ToUpperInvariant();
        if (!AllowedExtensions.Select(x => x.ToUpperInvariant()).Contains(ext))
        {
            return Result.Failure<UploadSegmentMasterDataResponse>(Error.Problem(
                "File.InvalidType",
                "Only .xlsx files are allowed for SegmentMasterData."));
        }

        if (!string.Equals(command.FileName, ExpectedFileName, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<UploadSegmentMasterDataResponse>(Error.Problem(
                "File.InvalidName",
                $"The file must be named '{ExpectedFileName}'."));
        }

        string configuredPath = configuration["SegmentMasterDataPath"];
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return Result.Failure<UploadSegmentMasterDataResponse>(Error.Problem(
                "Configuration.Missing",
                "SegmentMasterDataPath is not configured."));
        }

        string expandedPath = Environment.ExpandEnvironmentVariables(configuredPath);
        
        string directoryPath = Path.GetDirectoryName(expandedPath);
        if (!string.IsNullOrWhiteSpace(directoryPath) && !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
            logger.LogInformation("Created directory: {DirectoryPath}", directoryPath);
        }

        try
        {
            await File.WriteAllBytesAsync(expandedPath, command.Content, cancellationToken);
            logger.LogInformation("SegmentMasterData.xlsx uploaded successfully to: {FilePath}", expandedPath);

            int recordsLoaded = await RefreshSegmentMasterDataAsync(expandedPath, cancellationToken);

            return new UploadSegmentMasterDataResponse(
                FileName: command.FileName,
                FilePath: expandedPath,
                Size: command.Content.Length,
                RecordsLoaded: recordsLoaded,
                DatabaseRefreshed: true
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process SegmentMasterData.xlsx: {Error}", ex.Message);
            return Result.Failure<UploadSegmentMasterDataResponse>(Error.Problem(
                "File.ProcessFailed",
                $"Failed to process the file: {ex.Message}"));
        }
    }

    /// <summary>
    /// Refreshes the SegmentMaster data by clearing existing records and loading new data from the Excel file.
    /// </summary>
    /// <returns>The number of records loaded from the Excel file.</returns>
    private async Task<int> RefreshSegmentMasterDataAsync(string excelPath, CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting SegmentMaster data refresh from {FilePath}", excelPath);

        logger.LogInformation("Clearing existing SegmentMaster data...");
        await dbContext.SegmentMasters.ExecuteDeleteAsync(cancellationToken);
        logger.LogInformation("Existing SegmentMaster data cleared.");

        logger.LogInformation("Loading new SegmentMaster data from Excel file...");
        
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using ExcelPackage package = new(excelPath);
        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

        Dictionary<string, List<string>> segmentDict = new();

        int rowCount = worksheet.Dimension.Rows;
        for (int row = 2; row <= rowCount; row++)
        {
            string segment = worksheet.Cells[row, 1].Text.Trim();
            string subsegment = worksheet.Cells[row, 2].Text.Trim();

            if (string.IsNullOrWhiteSpace(segment) || string.IsNullOrWhiteSpace(subsegment))
            {
                continue;
            }

            if (!segmentDict.TryGetValue(segment, out List<string>? subSegments))
            {
                subSegments = new List<string>();
                segmentDict[segment] = subSegments;
            }

            subSegments.Add(subsegment);
        }

        var entities = segmentDict
            .Select(kvp => new SegmentMaster
            {
                Id = Guid.NewGuid(),
                Segment = kvp.Key,
                SubSegments = kvp.Value,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            })
            .ToList();

        dbContext.SegmentMasters.AddRange(entities);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Successfully loaded {Count} SegmentMaster records from uploaded file.", entities.Count);
        
        return entities.Count;
    }
}