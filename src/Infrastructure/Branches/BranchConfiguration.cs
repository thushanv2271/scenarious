using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Branches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Branches;

/// <summary>
/// Configures the Branch table in the database
/// Defines columns, constraints, indexes, and relationships
/// </summary>
internal sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.OrganizationId)
            .IsRequired();

        builder.Property(b => b.BranchName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(b => b.BranchCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(b => b.Email)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.ContactNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(b => b.Address)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(b => b.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.Property(b => b.UpdatedAt)
            .IsRequired();

        // Foreign key relationship
        builder.HasOne<Domain.Organizations.Organization>()
            .WithMany()
            .HasForeignKey(b => b.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_branches_organizations_organization_id");

        // Indexes
        builder.HasIndex(b => b.OrganizationId)
            .HasDatabaseName("ix_branches_organization_id");

        builder.HasIndex(b => b.BranchCode)
            .IsUnique()
            .HasDatabaseName("ix_branches_branch_code");

        builder.HasIndex(b => b.IsActive)
            .HasDatabaseName("ix_branches_is_active");

        builder.ToTable("branches");
    }
}
