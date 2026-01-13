namespace Application.PD.GetPDProgress;

/// <summary>
/// DTO representing a PD progress tracking record
/// </summary>
public sealed record PDProgressDto(
    Guid Id,
    Guid SessionId,
    string StepName,
    int StepOrder,
    string SubTaskName,
    int SubTaskOrder,
    bool IsActive,
    string Status,
    string? Message,
    DateTime CreatedAt,
    string CreatedBy
);

/// <summary>
/// Response wrapper that includes progress data and metadata
/// </summary>
public sealed record GetPDProgressResponse(
    IReadOnlyList<PDProgressDto> ProgressData,
    bool IsNewlyCreated,
    bool IsRerun,
    Guid SessionId,
    bool IsComplete,
    bool IsError
);
