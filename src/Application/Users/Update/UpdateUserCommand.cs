using Application.Abstractions.Messaging;
using Domain.Users;

namespace Application.Users.Update;

public sealed record UpdateUserCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    UserStatus UserStatus,
    List<Guid> RoleIds,
    Guid? BranchId
) : ICommand;
