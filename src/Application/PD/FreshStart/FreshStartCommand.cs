using Application.Abstractions.Messaging;

namespace Application.PD.FreshStart;

/// <summary>
/// Command to perform a fresh start by truncating specific database tables and clearing file storage
/// </summary>
/// <param name="CreatedBy">User who initiated the fresh start operation</param>
public sealed record FreshStartCommand(string CreatedBy) : ICommand;