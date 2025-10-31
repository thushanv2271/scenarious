using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Organizations;

internal sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{

    /// <summary>
    /// Entity Framework configuration for Organization entity
    /// </summary>
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(o => o.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.Email)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.ContactNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(o => o.Address)
            .IsRequired();

        builder.Property(o => o.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.Property(o => o.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(o => o.Code)
            .IsUnique()
            .HasDatabaseName("IX_Organizations_Code");

        builder.HasIndex(o => o.Email)
            .IsUnique()
            .HasDatabaseName("IX_Organizations_Email");

        builder.HasIndex(o => o.IsActive)
            .HasDatabaseName("IX_Organizations_IsActive");

        // Table name
        builder.ToTable("organizations");
    }
}
