using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.PDProgressTrackings;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.PD.GetPDProgress;

/// <summary>
/// Handles retrieval and initialization of PD progress tracking records
/// </summary>
internal sealed class GetPDProgressQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<GetPDProgressQuery, GetPDProgressResponse>
{
    public async Task<Result<GetPDProgressResponse>> Handle(
        GetPDProgressQuery request, 
        CancellationToken cancellationToken)
    {
        // Case 1: IsRerun = true -> Deactivate existing and create new
        if (request.IsRerun == true)
        {
            return await HandleRerun(cancellationToken);
        }

        // Case 2: IsRerun = false or null -> Return existing or create new
        return await HandleInitializeOrGet(cancellationToken);
    }

    private async Task<Result<GetPDProgressResponse>> HandleRerun(
        CancellationToken cancellationToken)
    {
        // Deactivate all existing active records
        List<PDProgressTracking> activeRecords = await context.PDProgressTrackings
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);

        foreach (PDProgressTracking record in activeRecords)
        {
            record.IsActive = false;
            record.UpdatedAt = DateTime.UtcNow;
            record.UpdatedBy = userContext.UserId.ToString();
        }

        // Create new records
        List<PDProgressTracking> newRecords = CreateProgressRecords();
        await context.PDProgressTrackings.AddRangeAsync(newRecords, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        // Return the new records with metadata
        IReadOnlyList<PDProgressDto> dtos = newRecords.Select(MapToDto).ToList();
        GetPDProgressResponse response = new(
            ProgressData: dtos,
            IsNewlyCreated: false,
            IsRerun: true,
            SessionId: newRecords[0].SessionId
        );
        return Result.Success(response);
    }

    private async Task<Result<GetPDProgressResponse>> HandleInitializeOrGet(
        CancellationToken cancellationToken)
    {
        // Check if active records exist
        List<PDProgressTracking> existingRecords = await context.PDProgressTrackings
            .Where(p => p.IsActive)
            .OrderBy(p => p.StepOrder)
            .ThenBy(p => p.SubTaskOrder)
            .ToListAsync(cancellationToken);

        // If records exist, return them
        if (existingRecords.Any())
        {
            bool isNewlyCreated = existingRecords.All(r => r.Status == PDProgressStatus.Pending);
            
            IReadOnlyList<PDProgressDto> existingDtos = existingRecords.Select(MapToDto).ToList();
            GetPDProgressResponse response = new(
                ProgressData: existingDtos,
                IsNewlyCreated: isNewlyCreated,
                IsRerun: false,
                SessionId: existingRecords[0].SessionId
            );
            return Result.Success(response);
        }

        // No active records exist, create new ones
        List<PDProgressTracking> newRecords = CreateProgressRecords();
        await context.PDProgressTrackings.AddRangeAsync(newRecords, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        IReadOnlyList<PDProgressDto> newDtos = newRecords.Select(MapToDto).ToList();
        GetPDProgressResponse newResponse = new(
            ProgressData: newDtos,
            IsNewlyCreated: true,
            IsRerun: false,
            SessionId: newRecords[0].SessionId
        );
        return Result.Success(newResponse);
    }

    private List<PDProgressTracking> CreateProgressRecords()
    {
        var sessionId = Guid.NewGuid();
        List<PDStepDefinition> steps = PDStepsConfiguration.GetDefaultSteps();
        var records = new List<PDProgressTracking>();

        foreach (PDStepDefinition step in steps)
        {
            foreach (PDSubTaskDefinition subTask in step.SubTasks)
            {
                var record = new PDProgressTracking
                {
                    Id = Guid.NewGuid(),
                    SessionId = sessionId,
                    StepName = step.StepName,
                    StepOrder = step.StepOrder,
                    SubTaskName = subTask.SubTaskName,
                    SubTaskOrder = subTask.SubTaskOrder,
                    IsActive = true,
                    Status = PDProgressStatus.Pending,
                    Message = null,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userContext.UserId.ToString(),
                    UpdatedAt = null,
                    UpdatedBy = null
                };

                records.Add(record);
            }
        }

        return records;
    }

    private static PDProgressDto MapToDto(PDProgressTracking entity)
    {
        return new PDProgressDto(
            Id: entity.Id,
            SessionId: entity.SessionId,
            StepName: entity.StepName,
            StepOrder: entity.StepOrder,
            SubTaskName: entity.SubTaskName,
            SubTaskOrder: entity.SubTaskOrder,
            IsActive: entity.IsActive,
            Status: entity.Status.ToString(),
            Message: entity.Message,
            CreatedAt: entity.CreatedAt,
            CreatedBy: entity.CreatedBy
        );
    }
}
