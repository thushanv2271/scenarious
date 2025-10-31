namespace SharedKernel;

/// <summary>
/// Static registry containing all hardcoded permissions in the system
/// </summary>
/// 
public class CategoryInfo
{
    public string Category { get; set; }
    public string CategoryName { get; set; }
}

public static class PermissionRegistry
{


    #region Category Constants
    public static readonly CategoryInfo CategoryAdminDashboard = new CategoryInfo //Dashboard
    {
        Category = "Dashboard",
        CategoryName = "Dashboard"
    };
    public static readonly CategoryInfo CategoryAdminUserManagement = new CategoryInfo //user managment
    {
        Category = "UserManagement",
        CategoryName = "User Management"
    };
    public static readonly CategoryInfo CategoryAdminSettingsProfile = new CategoryInfo //setting profile
    {
        Category = "Profile",
        CategoryName = "Profile"
    };
    public static readonly CategoryInfo CategoryAdminSettingsPassword = new CategoryInfo //settings Password
    {
        Category = "Password",
        CategoryName = "Password"
    };
    public static readonly CategoryInfo CategoryAdminSettingsRolePermission = new CategoryInfo //settings role permission
    {
        Category = "RolePermission",
        CategoryName = "Role Permission"
    };
    public static readonly CategoryInfo PD = new CategoryInfo //PD related permissions
    {
        Category = "PD",
        CategoryName = "PD"
    };


    public static readonly CategoryInfo CategoryUsers = new CategoryInfo //General
    {
        Category = "Users",
        CategoryName = "Users"
    };




    #endregion

    #region Admin Permissions
    //dashboard
    public const string AdminDashboardRead = "Admin.Dashboard.Read";

    //user managment
    public const string AdminUserManagementRead = "Admin.UserManagement.Read";
    public const string AdminUserManagementEdit = "Admin.UserManagement.Edit";
    public const string AdminUserManagementDelete = "Admin.UserManagement.Delete";
    public const string AdminUserManagementCreate = "Admin.UserManagement.Create";

    //settings
    public const string AdminSettingsProfileRead = "Admin.Settings.Profile.Read";
    public const string AdminSettingsProfileEdit = "Admin.Settings.Profile.Edit";
    public const string AdminSettingsPasswordChange = "Admin.Settings.Password.Change";

    public const string AdminSettingsRolePermissionRead = "Admin.Settings.RolePermission.Read";
    public const string AdminSettingsRolePermissionEdit = "Admin.Settings.RolePermission.Edit";
    public const string AdminSettingsRolePermissionDelete = "Admin.Settings.RolePermission.Delete";
    public const string AdminSettingsRolePermissionCreate = "Admin.Settings.RolePermission.Create";

    public const string PDSetupAccess = "PD.Setup.Create";

    #endregion

    #region General
    public const string UsersAccess = "Users.Access";
    #endregion

    /// <summary>
    /// Get all permissions with their metadata for seeding and tree display
    /// </summary>
    public static IReadOnlyList<PermissionDefinition> GetAllPermissions()
    {
        return new List<PermissionDefinition>
        {

            // Admin Permissions
            // **Dashboard
            new(AdminDashboardRead, "View Dashboard", CategoryAdminDashboard.Category,CategoryAdminDashboard.CategoryName, "Allows View Dashboard"),

            //**Usermanagement
            new(AdminUserManagementCreate, "Create User", CategoryAdminUserManagement.Category,CategoryAdminUserManagement.CategoryName, "Allows creating new users"),
            new(AdminUserManagementRead, "View Users", CategoryAdminUserManagement.Category,CategoryAdminUserManagement.CategoryName, "Allows viewing user information"),
            new(AdminUserManagementEdit, "Update User", CategoryAdminUserManagement.Category,CategoryAdminUserManagement.CategoryName, "Allows updating user information"),
            new(AdminUserManagementDelete, "Delete User", CategoryAdminUserManagement.Category,CategoryAdminUserManagement.CategoryName, "Allows deleting users"),

            //**Profile
            new(AdminSettingsProfileRead, "Profile Read", CategoryAdminSettingsProfile.Category,CategoryAdminSettingsProfile.CategoryName, "Allows Read user Profiles"),
            new(AdminSettingsProfileEdit, "Profile Edit", CategoryAdminSettingsProfile.Category,CategoryAdminSettingsProfile.CategoryName, "Allows Edit user Profiles"),

            //** Password
            new(AdminSettingsPasswordChange, "Change Password", CategoryAdminSettingsPassword.Category,CategoryAdminSettingsPassword.CategoryName, "Allows Change user Password"),

            //**Role Managment
            new(AdminSettingsRolePermissionCreate, "Create Role", CategoryAdminSettingsRolePermission.Category,CategoryAdminSettingsRolePermission.CategoryName, "Allows creating new roles"),
            new(AdminSettingsRolePermissionRead, "View Roles", CategoryAdminSettingsRolePermission.Category,CategoryAdminSettingsRolePermission.CategoryName, "Allows viewing role information"),
            new(AdminSettingsRolePermissionEdit, "Update Role", CategoryAdminSettingsRolePermission.Category,CategoryAdminSettingsRolePermission.CategoryName, "Allows updating role information"),
            new(AdminSettingsRolePermissionDelete, "Delete Role", CategoryAdminSettingsRolePermission.Category,CategoryAdminSettingsRolePermission.CategoryName, "Allows deleting roles"),

            //** PD
            new(PDSetupAccess, "PD Setup Access", PD.Category,PD.CategoryName, "Access to PD setup functionalities"),

            //gemeral
            new(UsersAccess, "User Access", CategoryUsers.Category,CategoryUsers.CategoryName, "General user access permissions")
        };
    }

    /// <summary>
    /// Get permissions grouped by category for tree display
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<PermissionDefinition>> GetPermissionsByCategory()
    {
        return GetAllPermissions()
            .GroupBy(p => p.Category)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<PermissionDefinition>)g.ToList()
            );
    }

    /// <summary>
    /// Validate if a permission key exists
    /// </summary>
    public static bool IsValidPermission(string permissionKey)
    {
        return GetAllPermissions().Any(p => p.Key == permissionKey);
    }
}

/// <summary>
/// Represents a permission definition with metadata
/// </summary>
public sealed record PermissionDefinition(
    string Key,
    string DisplayName,
    string Category,
    string CategoryName,
    string Description
);
