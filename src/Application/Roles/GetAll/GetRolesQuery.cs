using Application.Abstractions.Messaging;

namespace Application.Roles.GetAll;

public sealed record GetRolesQuery : IQuery<List<RoleListResponse>>;
