namespace Application.Abstractions.Progress;

public interface IProgressPublisher<in TStatus> where TStatus : Enum
{
    Task PublishProgress(
        Guid sessionId,
        int stepOrder,
        int subTaskOrder,
        TStatus status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);
}