namespace Application.Permissions.GetPermissionTree;

public sealed record PermissionTreeResponse(IReadOnlyList<PermissionTreeNode> Permissions);

public sealed record PermissionTreeNode(
    string Key,
    string DisplayName,
    string Category,
    string? Description,
    bool IsPresent);
