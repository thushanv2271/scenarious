using Application.Abstractions.Messaging;
using Domain.Users;

namespace Application.Users.ChangeStatus;

public sealed record ChangeUserStatusCommand(Guid UserId, UserStatus NewStatus) : ICommand;
