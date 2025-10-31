using Domain.Users;

namespace Application.Users.GetAll;

public sealed record UserListResponse
{
    public Guid Id { get; init; }

    public string Email { get; init; }

    public string FirstName { get; init; }

    public string LastName { get; init; }

    public List<Guid> RoleIds { get; set; } = new();
    public string UserStatus { get; set; }

    public Guid? BranchId { get; init; }
    public string? BranchName { get; init; }
    public string? BranchCode { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime ModifiedAt { get; init; }
}
