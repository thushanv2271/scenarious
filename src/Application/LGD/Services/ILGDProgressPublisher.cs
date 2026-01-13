using Application.Abstractions.Progress;
using Domain.LGDProgressTrackings;

namespace Application.LGD.Services;

/// <summary>
/// Service for publishing LGD progress updates to Redis stream
/// </summary>
public interface ILgdProgressPublisher : IProgressPublisher<LgdProgressStatus> { }
