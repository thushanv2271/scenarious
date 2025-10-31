using Application.Abstractions.Data;
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
using Infrastructure.DomainEvents;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Infrastructure.Database;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IDomainEventsDispatcher domainEventsDispatcher)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users { get; set; }

    public DbSet<TodoItem> TodoItems { get; set; }

    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public DbSet<RefreshToken> RefreshTokens { get; set; }

    // Role-based permission system
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<ExportAudit> ExportAudits { get; set; } = null!;

    public DbSet<PDTempData> PDTempDatas { get; set; } = null!;

    public DbSet<SegmentMaster> SegmentMasters { get; set; } = null!;

    public DbSet<UploadedFile> UploadedFiles { get; set; } = null!;

    public DbSet<EfaConfiguration> EfaConfigurations { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Branch> Branches { get; set; }

    public DbSet<ProductCategory> ProductCategories { get; set; }
    public DbSet<Segment> Segments { get; set; }
    public DbSet<Scenario> Scenarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.HasDefaultSchema(Schemas.Default);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // When should you publish domain events?
        //
        // 1. BEFORE calling SaveChangesAsync
        //     - domain events are part of the same transaction
        //     - immediate consistency
        // 2. AFTER calling SaveChangesAsync
        //     - domain events are a separate transaction
        //     - eventual consistency
        //     - handlers can fail

        int result = await base.SaveChangesAsync(cancellationToken);

        await PublishDomainEventsAsync();

        return result;
    }

    private async Task PublishDomainEventsAsync()
    {
        var domainEvents = ChangeTracker
            .Entries<Entity>()
            .Select(entry => entry.Entity)
            .SelectMany(entity =>
            {
                List<IDomainEvent> domainEvents = entity.DomainEvents;

                entity.ClearDomainEvents();

                return domainEvents;
            })
            .ToList();

        await domainEventsDispatcher.DispatchAsync(domainEvents);
    }
}
