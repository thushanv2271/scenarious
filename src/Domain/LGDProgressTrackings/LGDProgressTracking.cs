using SharedKernel;

namespace Domain.LGDProgressTrackings;

/// <summary>
/// Represents the progress tracking for LGD calculation steps and sub-steps
/// </summary>
public sealed class LgdProgressTracking : Entity
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public string SubTaskName { get; set; } = string.Empty;
    public int SubTaskOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public LgdProgressStatus Status { get; set; } = LgdProgressStatus.Pending;
    public string? Message { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
