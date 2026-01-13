using Application.LGD.DTOs;

namespace Application.LGD.GetLGDProgress;

/// <summary>
/// DTO for LGD progress tracking data
/// </summary>
public sealed record LgdProgressDto(
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
/// Response containing LGD progress tracking data and metadata
/// </summary>
public sealed record GetLgdProgressResponse(
    IReadOnlyList<LgdProgressDto> ProgressData,
    bool IsNewlyCreated,
    bool IsRerun,
    Guid SessionId
);