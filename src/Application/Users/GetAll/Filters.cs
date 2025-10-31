using Domain.Users;

namespace Application.Users.GetAll;

public sealed record UserFilters
{
    public UserStatus? Status { get; init; }
    public Guid[] RoleIds { get; init; } = Array.Empty<Guid>();
    public Guid[] BranchIds { get; init; } = Array.Empty<Guid>();
    public DateRange? CreatedDateRange { get; init; }
    public DateRange? ModifiedDateRange { get; init; }
    public Guid[] UserIds { get; init; } = Array.Empty<Guid>();
}

public sealed record DateRange(DateTime Start, DateTime End)
{
    public DateTime Start { get; init; } = DateTime.SpecifyKind(Start.Date, DateTimeKind.Utc);
    public DateTime End { get; init; } = DateTime.SpecifyKind(End.Date, DateTimeKind.Utc);
}
