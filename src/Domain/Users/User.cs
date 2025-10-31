using Domain.Branches;
using SharedKernel;

namespace Domain.Users;

public sealed class User : Entity
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PasswordHash { get; set; }
    public UserStatus UserStatus { get; set; }
    public bool IsTemporaryPassword { get; set; }
    public bool IsWizardComplete { get; set; }

    public Guid? BranchId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public Branch? Branch { get; set; }
}
