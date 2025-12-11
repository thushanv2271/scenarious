namespace Application.PD.DTOs;

/// <summary>
/// DTO for SignalR real-time progress updates sent to frontend
/// </summary>
public record PDProgressUpdateDto(
    Guid SessionId,
    int StepOrder,
    string StepName,
    int SubTaskOrder,
    string SubTaskName,
    string Status,
    string? ErrorMessage,
    DateTime Timestamp
);
