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
        await SeedRiskIndicatorsAsync(cancellationToken);

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


    private async Task SeedRiskIndicatorsAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Seeding Risk Indicators...");

        // ?? Always clear old indicators before reseeding (prevents duplicate tracking)
        if (await context.RiskIndicators.AnyAsync(cancellationToken))
        {
            if (await context.CustomerRiskIndicatorEvaluations.AnyAsync(cancellationToken))
            {
                logger.LogWarning("Cannot reseed Risk Indicators - evaluations exist. Skipping...");
                return;
            }

            logger.LogInformation("Clearing existing Risk Indicators before reseeding...");
            context.RiskIndicators.RemoveRange(context.RiskIndicators);
            await context.SaveChangesAsync(cancellationToken);
        }

        DateTime now = DateTime.UtcNow;

        var indicators = new List<Domain.RiskEvaluations.RiskIndicator>
    {
        // Significant Increase in Credit Risk (SICR)
        new()
        {
            IndicatorId = Guid.CreateVersion7(), // ? ensure unique key
            Category = Domain.RiskEvaluations.RiskIndicatorCategory.SICR,
            Description = "Contractual payments of a customer are more than 30 days past due",
            PossibleValues = "Yes,No,N/A",
            DisplayOrder = 1,
            CreatedAt = now
        },
        new()
        {
            IndicatorId = Guid.CreateVersion7(),
            Category = Domain.RiskEvaluations.RiskIndicatorCategory.SICR,
            Description = "Risk rating of a customer or an instrument has been downgraded to B+ by an external credit rating agency and/or internal rating (Sovereign to less: switch)",
            PossibleValues = "Yes,No,N/A",
            DisplayOrder = 2,
            CreatedAt = now
        },
        new()
        {
            IndicatorId = Guid.CreateVersion7(),
            Category = Domain.RiskEvaluations.RiskIndicatorCategory.SICR,
            Description = "Reasonable and supportable forecasts of future economic conditions directly negatively affect the performance of a customer/group of customers, portfolios or instruments",
            PossibleValues = "Yes,No,N/A",
            DisplayOrder = 3,
            CreatedAt = now
        },
        new()
        {
            IndicatorId = Guid.CreateVersion7(),
            Category = Domain.RiskEvaluations.RiskIndicatorCategory.SICR,
            Description = "A significant change in the geographical location of natural catastrophes that directly impact the performance of a customer/group of customers or an instrument",
            PossibleValues = "Yes,No,N/A",
            DisplayOrder = 4,
            CreatedAt = now
        },
        new()
        {
            IndicatorId = Guid.CreateVersion7(),
            Category = Domain.RiskEvaluations.RiskIndicatorCategory.SICR,
            Description = "The value of collateral is significantly reduced and/or realizability of collateral is doubtful (units must be set and documented by business/credit team)",
            PossibleValues = "Yes,No,N/A",
            DisplayOrder = 5,
            CreatedAt = now
        },
        new()
        {
            IndicatorId = Guid.CreateVersion7(),
            Category = Domain.RiskEvaluations.RiskIndicatorCategory.SICR,
            Description = "If customer is subject to litigation that significantly affects the performance of the credit facility",
            PossibleValues = "Yes,No,N/A",
            DisplayOrder = 6,
            CreatedAt = now
        },
        new()
        {
            IndicatorId = Guid.CreateVersion7(),
            Category = Domain.RiskEvaluations.RiskIndicatorCategory.SICR,
            Description = "Frequent changes in the Board of Directors and the senior management of an institutional customer",
            PossibleValues = "Yes,No,N/A",
            DisplayOrder = 7,
            CreatedAt = now
        },
        new()
        {
            IndicatorId = Guid.CreateVersion7(),
            Category = Domain.RiskEvaluations.RiskIndicatorCategory.SICR,
            Description = "Delay in the commencement of business operations/projects by more than two years from the originally agreed date",
            PossibleValues = "Yes,No,N/A",
            DisplayOrder = 8,
            CreatedAt = now
        },
        new()
        {
            IndicatorId = Guid.CreateVersion7(),
            Category = Domain.RiskEvaluations.RiskIndicatorCategory.SICR,
            Description = "Modification of terms resulting in concessions, including extensions, deferment of payments, waiver of covenants",
            PossibleValues = "Yes,No,N/A",
            DisplayOrder = 9,
            CreatedAt = now
        },
        new()
        {
            IndicatorId = Guid.CreateVersion7(),
            Category = Domain.RiskEvaluations.RiskIndicatorCategory.SICR,
            Description = "When the bank is unable to contact or find the customer",
            PossibleValues = "Yes,No,N/A",
            DisplayOrder = 10,
            CreatedAt = now
        },
        new()
        {
            IndicatorId = Guid.CreateVersion7(),
            Category = Domain.RiskEvaluations.RiskIndicatorCategory.SICR,
            Description = "A fall of 10% or more in the turnover and/or profit before tax of the customer when compared to the previous year",
            PossibleValues = "Yes,No,N/A",
            DisplayOrder = 11,
            CreatedAt = now
        },
        new()
        {
            IndicatorId = Guid.CreateVersion7(),
            Category = Domain.RiskEvaluations.RiskIndicatorCategory.SICR,
            Description = "Erosion in net worth by more than 25% when compared to the previous year",
            PossibleValues = "Yes,No,N/A",
            DisplayOrder = 12,
            CreatedAt = now
        },

        // Objective Evidence of Incurred Loss (OEIL)
        new()
        {
            IndicatorId = Guid.CreateVersion7(),
            Category = Domain.RiskEvaluations.RiskIndicatorCategory.OEIL,
            Description = "Significant financial difficulty of the issuer/obligor",
            PossibleValues = "Yes,No,N/A",
            DisplayOrder = 1,
            CreatedAt = now
        },
        new()
        {
            IndicatorId = Guid.CreateVersion7(),
            Category = Domain.RiskEvaluations.RiskIndicatorCategory.OEIL,
            Description = "A breach of contract, such as a default or delinquency in interest or principal payment",
            PossibleValues = "Yes,No,N/A",
            DisplayOrder = 2,
            CreatedAt = now
        },
        new()
        {
            IndicatorId = Guid.CreateVersion7(),
            Category = Domain.RiskEvaluations.RiskIndicatorCategory.OEIL,
            Description = "The lender for economic or legal reasons relating to the borrower's financial difficulty granting to the borrower a concession that the lender would not otherwise consider",
            PossibleValues = "Yes,No,N/A",
            DisplayOrder = 3,
            CreatedAt = now
        },
        new()
        {
            IndicatorId = Guid.CreateVersion7(),
            Category = Domain.RiskEvaluations.RiskIndicatorCategory.OEIL,
            Description = "High probability of bankruptcy or other financial reorganization",
            PossibleValues = "Yes,No,N/A",
            DisplayOrder = 4,
            CreatedAt = now
        },
        new()
        {
            IndicatorId = Guid.CreateVersion7(),
            Category = Domain.RiskEvaluations.RiskIndicatorCategory.OEIL,
            Description = "Disappearance of an active market for the security (This is due to the financial difficulties of the issuer or that of third security)",
            PossibleValues = "Yes,No,N/A",
            DisplayOrder = 5,
            CreatedAt = now
        }
    };

        context.RiskIndicators.AddRange(indicators);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("? Seeded {Count} Risk Indicators successfully.", indicators.Count);
    }




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
