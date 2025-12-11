using Application.Abstractions.Messaging;

namespace Application.PD.SimulatePD;

/// <summary>
/// Command to simulate PD calculation progress for testing purposes
/// </summary>
public sealed record SimulatePDCommand(
    Guid SessionId,
    int DelayMilliseconds = 2000
) : ICommand;
