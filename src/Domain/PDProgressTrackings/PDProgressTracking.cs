using SharedKernel;

namespace Domain.PDProgressTrackings;

/// <summary>
/// Represents the progress tracking for PD algorithm steps and sub-steps
/// </summary>
public sealed class PDProgressTracking : Entity
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public string SubTaskName { get; set; } = string.Empty;
    public int SubTaskOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public PDProgressStatus Status { get; set; } = PDProgressStatus.Pending;
    public string? Message { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
