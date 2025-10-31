using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Domain.Branches;
using Domain.MasterData;
using Domain.Organizations;
using Domain.Permissions;
using Domain.ProductCategories;
using Domain.RolePermissions;
using Domain.Roles;
using Domain.Segments;
using Domain.UserRoles;
using Domain.Users;
using Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using SharedKernel;

namespace Infrastructure.Database.Seeding;

public sealed class DatabaseSeeder(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    ILogger<DatabaseSeeder> logger)
{
    private const string AdministratorRoleName = "Administrator";
    private const string AdminEmail = "admin@saral.com";
    private const string AdminPass = "Admin123!";
    private readonly string AdminPasswordHash = passwordHasher.Hash(AdminPass);

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting database seeding...");

        await SeedPermissionsAsync(cancellationToken);
        await SeedOrganizationsAsync(cancellationToken);
        await SeedBranchesAsync(cancellationToken);
        await SeedAdministratorRoleAndUserAsync(cancellationToken);
        await SeedSegmentMasterAsync(cancellationToken);
        await SeedProductCategoriesAsync(cancellationToken);
        await SeedSegmentsAsync(cancellationToken);

        logger.LogInformation("Database seeding completed successfully.");
    }

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Seeding permissions...");

        HashSet<string> existingPermissions = await context.Permissions
            .Select(p => p.Key)
            .ToHashSetAsync(cancellationToken);

        var permissionsToAdd = PermissionRegistry.GetAllPermissions()
            .Where(permissionDef => !existingPermissions.Contains(permissionDef.Key))
            .Select(permissionDef => new Permission(
                Guid.CreateVersion7(),
                permissionDef.Key,
                permissionDef.DisplayName,
                permissionDef.Category,
                permissionDef.Description))
            .ToList();

        if (permissionsToAdd.Count > 0)
        {
            context.Permissions.AddRange(permissionsToAdd);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Added {Count} new permissions", permissionsToAdd.Count);
        }
        else
        {
            logger.LogInformation("All permissions already exist, skipping permission seeding");
        }
    }

    private async Task SeedAdministratorRoleAndUserAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Seeding administrator role and user...");

        // Create Administrator role if it doesn't exist
        Role? adminRole = await context.Roles
            .FirstOrDefaultAsync(r => r.Name == AdministratorRoleName, cancellationToken);

        Branch? defaultBranch = await context.Branches
            .FirstOrDefaultAsync(b => b.BranchCode == "CMB001", cancellationToken);

        // Create or update admin user if it doesn't exist or lacks branch
        User? adminUser = await context.Users
            .FirstOrDefaultAsync(u => u.Email == AdminEmail, cancellationToken);

        if (adminUser is null)
        {
            adminUser = new User
            {
                Id = Guid.CreateVersion7(),
                Email = AdminEmail,
                FirstName = "System",
                LastName = "Administrator",
                PasswordHash = AdminPasswordHash,
                BranchId = defaultBranch?.Id, // Assign to default branch
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            context.Users.Add(adminUser);
            logger.LogInformation("Created admin user: {Email} assigned to branch: {BranchCode}",
                AdminEmail, defaultBranch?.BranchCode ?? "None");

            await context.SaveChangesAsync(cancellationToken);
        }
        else if (adminUser.BranchId == null && defaultBranch != null)
        {
            // Update existing admin user with branch
            adminUser.BranchId = defaultBranch.Id;
            adminUser.ModifiedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Updated admin user with branch: {BranchCode}", defaultBranch.BranchCode);
        }

        if (adminRole is null)
        {
            adminRole = new Role(
                Guid.CreateVersion7(),
                AdministratorRoleName,
                "System administrator with full access to all features",
                isSystemRole: true);

            context.Roles.Add(adminRole);
            logger.LogInformation("Created Administrator role");
            await context.SaveChangesAsync(cancellationToken);
        }

        // Assign all permissions to Administrator role
        List<Permission> allPermissions = await context.Permissions.ToListAsync(cancellationToken);
        List<Guid> existingRolePermissionIds = await context.RolePermissions
            .Where(rp => rp.RoleId == adminRole.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken);

        var newRolePermissions = allPermissions
            .Where(p => !existingRolePermissionIds.Contains(p.Id))
            .Select(p => new RolePermission(adminRole.Id, p.Id))
            .ToList();

        if (newRolePermissions.Count > 0)
        {
            context.RolePermissions.AddRange(newRolePermissions);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Assigned {Count} permissions to Administrator role", newRolePermissions.Count);
        }

        // Assign Administrator role to admin user if not already assigned
        bool hasAdminRole = await context.UserRoles
            .AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRole.Id, cancellationToken);

        if (!hasAdminRole)
        {
            UserRole userRole = new(adminUser.Id, adminRole.Id);
            context.UserRoles.Add(userRole);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Assigned Administrator role to admin user");
        }
    }

    private async Task SeedSegmentMasterAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Seeding SegmentMaster data...");

        bool hasSegments = await context.SegmentMasters.AnyAsync(cancellationToken);
        if (hasSegments)
        {
            logger.LogInformation("SegmentMaster data already exists, skipping seeding.");
            return;
        }

        IConfigurationRoot config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        string excelPath = config["SegmentMasterDataPath"];
        if (string.IsNullOrWhiteSpace(excelPath) || !File.Exists(excelPath))
        {
            logger.LogWarning("SegmentMasterDataPath not found or file missing: {Path}", excelPath);
            return;
        }

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using ExcelPackage package = new(excelPath);
        ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

        Dictionary<string, List<string>> segmentDict = new();

        int rowCount = worksheet.Dimension.Rows;
        for (int row = 2; row <= rowCount; row++)
        {
            string segment = worksheet.Cells[row, 1].Text.Trim();
            string subsegment = worksheet.Cells[row, 2].Text.Trim();

            if (string.IsNullOrWhiteSpace(segment) || string.IsNullOrWhiteSpace(subsegment))
            {
                continue;
            }

            if (!segmentDict.TryGetValue(segment, out List<string> subSegments))
            {
                subSegments = new List<string>();
                segmentDict[segment] = subSegments;
            }

            subSegments.Add(subsegment);
        }

        var entities = segmentDict
            .Select(kvp => new SegmentMaster
            {
                Id = Guid.CreateVersion7(),
                Segment = kvp.Key,
                SubSegments = kvp.Value,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            })
            .ToList();

        context.SegmentMasters.AddRange(entities);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {Count} SegmentMaster records.", entities.Count);
    }

    //Seed Organizations Data  
    private async Task SeedOrganizationsAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Seeding Organizations data...");

        bool hasOrganizations = await context.Organizations.AnyAsync(cancellationToken);
        if (hasOrganizations)
        {
            logger.LogInformation("Organizations data already exists, skipping seeding.");
            return;
        }

        var organizations = new List<Organization>
        {
            new Organization
            {
                Id = Guid.CreateVersion7(),
                Name = "Azend Technologies",
                Code = "AZEND",
                Email = "info@azendtech.com",
                ContactNumber = "+94 71 234 5678",
                Address = "Colombo, Sri Lanka",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Organization
            {
                Id = Guid.CreateVersion7(),
                Name = "Cora Analytics",
                Code = "CORA",
                Email = "contact@cora.com",
                ContactNumber = "+44 20 7946 1234",
                Address = "London, UK",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.Organizations.AddRange(organizations);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {Count} Organizations.", organizations.Count);
    }

    #region Product Categories Seeding

    private async Task SeedProductCategoriesAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Seeding Product Categories...");

        string[] categoriesToSeed = new[]
        {
        "Housing Loan",
        "Personal Loan",
        "Term Loan"
    };

        foreach (string categoryName in categoriesToSeed)
        {
            bool exists = await context.ProductCategories
                .AnyAsync(pc => pc.Name == categoryName, cancellationToken);

            if (!exists)
            {
                var category = new ProductCategory
                {
                    Id = Guid.CreateVersion7(),
                    Name = categoryName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.ProductCategories.Add(category);
                logger.LogInformation("Created Product Category: {Name}", categoryName);
            }
            else
            {
                logger.LogInformation("Product Category '{Name}' already exists, skipping.", categoryName);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Product Categories seeding completed.");
    }

    #endregion

    #region Segments Seeding

    private async Task SeedSegmentsAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Seeding Segments...");

        // Get all product categories
        List<ProductCategory> productCategories = await context.ProductCategories
            .ToListAsync(cancellationToken);

        if (productCategories.Count == 0)
        {
            logger.LogWarning("No Product Categories found. Cannot seed Segments.");
            return;
        }

        string[] segmentNames = new[] { "3 Year Loan", "5 Year Loan", "7 Year Loan" };
        int segmentsCreated = 0;

        foreach (ProductCategory category in productCategories)
        {
            foreach (string segmentName in segmentNames)
            {
                // Check if this specific combination already exists
                bool exists = await context.Segments
                    .AnyAsync(s => s.ProductCategoryId == category.Id && s.Name == segmentName,
                        cancellationToken);

                if (!exists)
                {
                    var segment = new Segment
                    {
                        Id = Guid.CreateVersion7(),
                        ProductCategoryId = category.Id,
                        Name = segmentName,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    context.Segments.Add(segment);
                    segmentsCreated++;
                    logger.LogInformation("Created Segment: {SegmentName} for {CategoryName}",
                        segmentName, category.Name);
                }
                else
                {
                    logger.LogInformation("Segment '{SegmentName}' for '{CategoryName}' already exists, skipping.",
                        segmentName, category.Name);
                }
            }
        }

        if (segmentsCreated > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded {Count} new Segments.", segmentsCreated);
        }
        else
        {
            logger.LogInformation("All Segments already exist, no new segments created.");
        }
    }

    #endregion

    //Seed Branches Data  
    private async Task SeedBranchesAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Seeding Branches data...");

        bool hasBranches = await context.Branches.AnyAsync(cancellationToken);
        if (hasBranches)
        {
            logger.LogInformation("Branches data already exists, skipping seeding.");
            return;
        }

        // Get Azend Technologies organization
        Organization? azendOrg = await context.Organizations
            .FirstOrDefaultAsync(o => o.Code == "AZEND", cancellationToken);

        if (azendOrg == null)
        {
            logger.LogWarning("Azend Technologies organization not found. Skipping branch seeding.");
            return;
        }

        var branches = new List<Branch>
        {
            new Branch
            {
                Id = Guid.CreateVersion7(),
                OrganizationId = azendOrg.Id,
                BranchName = "Colombo Main Branch",
                BranchCode = "CMB001",
                Email = "colombo@azendtech.com",
                ContactNumber = "+94 11 222 3344",
                Address = "No. 45, Galle Road, Colombo",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Branch
            {
                Id = Guid.CreateVersion7(),
                OrganizationId = azendOrg.Id,
                BranchName = "Kandy Branch",
                BranchCode = "KDY002",
                Email = "kandy@azendtech.com",
                ContactNumber = "+94 81 223 4455",
                Address = "Peradeniya Road, Kandy",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.Branches.AddRange(branches);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {Count} Branches.", branches.Count);
    }
}
