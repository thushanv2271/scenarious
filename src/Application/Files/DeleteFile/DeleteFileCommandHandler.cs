using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.Files.DeleteFile;

/// <summary>
/// Handles the deletion of uploaded files, removing both the physical file and database metadata.
/// </summary>
/// <remarks>
/// This handler performs two critical operations for each file:
/// 1. Deletes the physical file from the file system
/// 2. Removes the file metadata from the database
/// 
/// If the physical file deletion fails, the operation continues to remove the database record
/// to prevent orphaned metadata.
/// </remarks>
internal sealed class DeleteFileCommandHandler(
    IApplicationDbContext dbContext,
    ILogger<DeleteFileCommandHandler> logger,
    IDateTimeProvider dateTimeProvider
) : ICommandHandler<DeleteFileCommand, List<DeleteFileResponse>>
{
    public async Task<Result<List<DeleteFileResponse>>> Handle(
        DeleteFileCommand command,
        CancellationToken cancellationToken)
    {
        if (command is null)
        {
            return Result.Failure<List<DeleteFileResponse>>(Error.NullValue);
        }

        if (command.Ids == null || command.Ids.Count == 0)
        {
            return Result.Failure<List<DeleteFileResponse>>(Error.Problem(
                "File.EmptyIds",
                "No file IDs were provided for deletion."));
        }

        var responses = new List<DeleteFileResponse>();

        // Fetch all files that match the provided IDs
        List<UploadedFile> uploadedFiles = await dbContext.UploadedFiles
            .Where(x => command.Ids.Contains(x.Id))
            .ToListAsync(cancellationToken);

        // Check for not found files and log warning
        var foundIds = uploadedFiles.Select(x => x.Id).ToHashSet();
        var notFoundIds = command.Ids.Where(id => !foundIds.Contains(id)).ToList();

        if (notFoundIds.Any())
        {
            logger.LogWarning(
                "Some files were not found: {NotFoundIds}. Requested: {RequestedCount}, Found: {FoundCount}",
                string.Join(", ", notFoundIds),
                command.Ids.Count,
                uploadedFiles.Count);
        }

        // Process each file for deletion
        foreach (UploadedFile uploadedFile in uploadedFiles)
        {
            // Store details for response before deletion
            var response = new DeleteFileResponse(
                uploadedFile.Id,
                uploadedFile.OriginalFileName,
                uploadedFile.StoredFileName,
                uploadedFile.Size,
                uploadedFile.PhysicalPath,
                dateTimeProvider.UtcNow,
                command.DeletedBy
            );

            // Delete physical file from disk
            bool physicalFileDeleted = false;
            try
            {
                if (File.Exists(uploadedFile.PhysicalPath))
                {
                    File.Delete(uploadedFile.PhysicalPath);
                    physicalFileDeleted = true;
                    logger.LogInformation(
                        "Physical file deleted at '{PhysicalPath}' by user {UserId}",
                        uploadedFile.PhysicalPath,
                        command.DeletedBy);
                }
                else
                {
                    logger.LogWarning(
                        "Physical file not found at '{PhysicalPath}'. Proceeding to delete metadata.",
                        uploadedFile.PhysicalPath);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to delete physical file at '{PhysicalPath}'. Proceeding to delete metadata.",
                    uploadedFile.PhysicalPath);
            }

            // Remove metadata from database
            dbContext.UploadedFiles.Remove(uploadedFile);

            logger.LogInformation(
                "File '{FileName}' (ID: {FileId}) deleted by user {UserId}. Physical file deleted: {PhysicalDeleted}",
                uploadedFile.OriginalFileName,
                uploadedFile.Id,
                command.DeletedBy,
                physicalFileDeleted);

            responses.Add(response);
        }

        // Save all changes to database
        await dbContext.SaveChangesAsync(cancellationToken);

        // If all requested files were not found, return error
        if (responses.Count == 0)
        {
            return Result.Failure<List<DeleteFileResponse>>(Error.NotFound(
                "File.NotFound",
                $"None of the files with the provided IDs were found."));
        }

        logger.LogInformation(
            "Successfully deleted {DeletedCount} out of {RequestedCount} files by user {UserId}",
            responses.Count,
            command.Ids.Count,
            command.DeletedBy);

        return Result.Success(responses);
    }
}
