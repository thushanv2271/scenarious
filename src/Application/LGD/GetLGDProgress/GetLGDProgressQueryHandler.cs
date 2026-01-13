using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.LGDProgressTrackings;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.LGD.GetLGDProgress;

/// <summary>
/// Handles retrieval and initialization of LGD progress tracking records
/// </summary>
internal sealed class GetLgdProgressQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<GetLgdProgressQuery, GetLgdProgressResponse>
{
    public async Task<Result<GetLgdProgressResponse>> Handle(
        GetLgdProgressQuery request,
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

    private async Task<Result<GetLgdProgressResponse>> HandleRerun(
        CancellationToken cancellationToken)
    {
        // Deactivate all existing active records
        List<LgdProgressTracking> activeRecords = await context.LgdProgressTrackings
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);

        foreach (LgdProgressTracking record in activeRecords)
        {
            record.IsActive = false;
            record.UpdatedAt = DateTime.UtcNow;
            record.UpdatedBy = userContext.UserId.ToString();
        }

        // Create new records
        List<LgdProgressTracking> newRecords = CreateProgressRecords();
        await context.LgdProgressTrackings.AddRangeAsync(newRecords, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        // Return the new records with metadata
        IReadOnlyList<LgdProgressDto> dtos = newRecords.Select(MapToDto).ToList();
        GetLgdProgressResponse response = new(
            ProgressData: dtos,
            IsNewlyCreated: false,
            IsRerun: true,
            SessionId: newRecords[0].SessionId
        );
        return Result.Success(response);
    }

    private async Task<Result<GetLgdProgressResponse>> HandleInitializeOrGet(
        CancellationToken cancellationToken)
    {
        // Check if active records exist
        List<LgdProgressTracking> existingRecords = await context.LgdProgressTrackings
            .Where(p => p.IsActive)
            .OrderBy(p => p.StepOrder)
            .ThenBy(p => p.SubTaskOrder)
            .ToListAsync(cancellationToken);

        // If records exist, return them
        if (existingRecords.Any())
        {
            bool isNewlyCreated = existingRecords.All(r => r.Status == LgdProgressStatus.Pending);

            IReadOnlyList<LgdProgressDto> existingDtos = existingRecords.Select(MapToDto).ToList();
            GetLgdProgressResponse response = new(
                ProgressData: existingDtos,
                IsNewlyCreated: isNewlyCreated,
                IsRerun: false,
                SessionId: existingRecords[0].SessionId
            );
            return Result.Success(response);
        }

        // No active records exist, create new ones
        List<LgdProgressTracking> newRecords = CreateProgressRecords();
        await context.LgdProgressTrackings.AddRangeAsync(newRecords, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        IReadOnlyList<LgdProgressDto> newDtos = newRecords.Select(MapToDto).ToList();
        GetLgdProgressResponse newResponse = new(
            ProgressData: newDtos,
            IsNewlyCreated: true,
            IsRerun: false,
            SessionId: newRecords[0].SessionId
        );
        return Result.Success(newResponse);
    }

    private List<LgdProgressTracking> CreateProgressRecords()
    {
        var sessionId = Guid.NewGuid();
        List<LgdStepDefinition> steps = LgdStepsConfiguration.GetDefaultSteps();
        var records = new List<LgdProgressTracking>();

        foreach (LgdStepDefinition step in steps)
        {
            foreach (LgdSubTaskDefinition subTask in step.SubTasks)
            {
                var record = new LgdProgressTracking
                {
                    Id = Guid.NewGuid(),
                    SessionId = sessionId,
                    StepName = step.StepName,
                    StepOrder = step.StepOrder,
                    SubTaskName = subTask.SubTaskName,
                    SubTaskOrder = subTask.SubTaskOrder,
                    IsActive = true,
                    Status = LgdProgressStatus.Pending,
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

    private static LgdProgressDto MapToDto(LgdProgressTracking entity)
    {
        return new LgdProgressDto(
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