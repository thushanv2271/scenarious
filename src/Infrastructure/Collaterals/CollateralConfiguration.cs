using Domain.Collaterals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Collaterals;

internal sealed class CollateralConfiguration : IEntityTypeConfiguration<Collateral>
{
    public void Configure(EntityTypeBuilder<Collateral> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        builder.HasIndex(c => c.Name)
            .IsUnique()
            .HasDatabaseName("ix_collaterals_name");

        builder.ToTable("collaterals");
    }
}