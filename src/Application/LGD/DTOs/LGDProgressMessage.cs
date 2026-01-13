using Domain.LGDProgressTrackings;

namespace Application.LGD.DTOs;

/// <summary>
/// Message published to Redis stream for LGD progress updates
/// </summary>
public record LgdProgressMessage(
    Guid SessionId,
    int StepOrder,
    string StepName,
    int SubTaskOrder,
    string SubTaskName,
    LgdProgressStatus Status,
    string? ErrorMessage,
    DateTime Timestamp
);
