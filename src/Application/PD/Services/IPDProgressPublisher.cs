using Application.Abstractions.Progress;
using Domain.PDProgressTrackings;

namespace Application.PD.Services;

/// <summary>
/// Service for publishing PD progress updates to Redis stream
/// </summary>
public interface IPDProgressPublisher : IProgressPublisher<PDProgressStatus>
{
    /// <summary>
    /// Marks all in-progress tasks for the given session as failed
    /// </summary>
    Task MarkInProgressAsFailedAsync(Guid sessionId, string errorMessage, CancellationToken cancellationToken = default);
}
