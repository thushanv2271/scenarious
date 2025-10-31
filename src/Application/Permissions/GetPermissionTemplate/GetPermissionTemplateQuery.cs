using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Permissions.GetPermissionTemplate;

public sealed record GetPermissionTemplateQuery : IQuery<IReadOnlyList<PermissionDefinition>>;
