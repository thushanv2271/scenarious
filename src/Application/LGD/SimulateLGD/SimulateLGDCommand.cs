using Application.Abstractions.Messaging;

namespace Application.LGD.SimulateLGD;

/// <summary>
/// Command for simulating LGD calculation progress
/// </summary>
public sealed record SimulateLgdCommand(Guid SessionId, int DelayMilliseconds) : ICommand;

/// <summary>
/// Request model for LGD simulation endpoint
/// </summary>
public sealed record SimulateLgdRequest(
    Guid SessionId,
    int DelayMilliseconds = 2000
);
