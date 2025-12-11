using Domain.PDProgressTrackings;

namespace Application.PD.DTOs;

/// <summary>
/// Message published to Redis stream for PD progress updates
/// </summary>
public record PDProgressMessage(
    Guid SessionId,
    int StepOrder,
    string StepName,
    int SubTaskOrder,
    string SubTaskName,
    PDProgressStatus Status,
    string? ErrorMessage,
    DateTime Timestamp
);
