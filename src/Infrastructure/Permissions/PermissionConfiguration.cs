using Domain.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Permissions;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Key)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.DisplayName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Category)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(p => p.Key)
            .IsUnique()
            .HasDatabaseName("IX_Permissions_Key");

        builder.HasIndex(p => p.Category)
            .HasDatabaseName("IX_Permissions_Category");

        // Table name
        builder.ToTable("Permissions");
    }
}
