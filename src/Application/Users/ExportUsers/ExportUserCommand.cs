using Application.Abstractions.Messaging;
using Application.Users.GetAll;
using SharedKernel;

namespace Application.Users.ExportUsers;

public sealed record ExportUserCommand(
    int PageNumber,
    int PageSize,
    string? Search,
    UserFilters? Filters,
    Guid RequestedBy
) : IQuery<byte[]>;
