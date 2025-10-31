using Application.Abstractions.Messaging;

namespace Application.Users.GetUserEffectivePermissions;

public sealed record GetUserEffectivePermissionsQuery(Guid UserId) : IQuery<HashSet<string>>;
