using Application.Abstractions.Messaging;

namespace Application.Users.Register;

/// <summary>
/// Command to register a new user in the system
/// Contains all the information needed to create a user account
/// </summary>
public sealed record RegisterUserCommand(
    string Email, 
    string FirstName, 
    string LastName, 
    List<Guid> RoleIds, 
    Guid? BranchId
    ): ICommand<Guid>; // Returns the ID of the newly created user
