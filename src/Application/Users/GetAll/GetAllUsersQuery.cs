using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Users.GetAll;

public sealed record GetAllUsersQuery(
    int PageNumber,
    int PageSize,
    string? Search,
    UserFilters? Filters
) : IQuery<PaginatedResult<UserListResponse>>;

