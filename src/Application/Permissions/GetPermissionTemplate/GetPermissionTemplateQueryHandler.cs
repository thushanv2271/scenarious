using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Permissions.GetPermissionTemplate;

internal sealed class GetAllPermissionsQueryHandler
    : IQueryHandler<GetPermissionTemplateQuery, IReadOnlyList<PermissionDefinition>>
{
    public Task<Result<IReadOnlyList<PermissionDefinition>>> Handle(
        GetPermissionTemplateQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PermissionDefinition> permissions = PermissionRegistry.GetAllPermissions();
        return Task.FromResult(Result.Success<IReadOnlyList<PermissionDefinition>>(permissions));
    }
}
