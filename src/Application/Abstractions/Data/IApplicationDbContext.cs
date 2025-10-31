using Domain.Authentication;
using Domain.Branches;
using Domain.EfaConfigs;
using Domain.Exports;
using Domain.Files;
using Domain.MasterData;
using Domain.Organizations;
using Domain.PasswordResetTokens;
using Domain.PDTempData;
using Domain.Permissions;
using Domain.ProductCategories;
using Domain.RolePermissions;
using Domain.Roles;
using Domain.Scenarios;
using Domain.Segments;
using Domain.Todos;
using Domain.UserRoles;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Abstractions.Data;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<TodoItem> TodoItems { get; }

    DbSet<PasswordResetToken> PasswordResetTokens { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    // Role-based permission system
    DbSet<Permission> Permissions { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<ExportAudit> ExportAudits { get; }

    DbSet<PDTempData> PDTempDatas { get; }

    DbSet<SegmentMaster> SegmentMasters { get; }

    DbSet<UploadedFile> UploadedFiles { get; }

    DbSet<EfaConfiguration> EfaConfigurations { get; }

    DbSet<Organization> Organizations { get; }
    DbSet<Branch> Branches { get; }

    DbSet<ProductCategory> ProductCategories { get; }
    DbSet<Segment> Segments { get; }
    DbSet<Scenario> Scenarios { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
