using Application.Abstractions.Progress;
using Domain.PDProgressTrackings;

namespace Application.PD.Services;

/// <summary>
/// Service for publishing PD progress updates to Redis stream
/// </summary>
public interface IPDProgressPublisher : IProgressPublisher<PDProgressStatus>{}
