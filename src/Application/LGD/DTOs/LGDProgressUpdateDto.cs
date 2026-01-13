namespace Application.LGD.DTOs;

/// <summary>
/// DTO for real-time LGD progress updates sent via SignalR
/// </summary>
public record LgdProgressUpdateDto(
    Guid SessionId,
    int StepOrder,
    string StepName,
    int SubTaskOrder,
    string SubTaskName,
    string Status,
    string? ErrorMessage,
    DateTime Timestamp
);
