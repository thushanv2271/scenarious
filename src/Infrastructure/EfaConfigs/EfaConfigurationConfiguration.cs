using Domain.EfaConfigs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EfaConfigs;

/// <summary>
/// Configures the EF Core mapping for the <see cref="EfaConfiguration"/> entity.
/// Defines keys, properties, indexes, and the table name.
/// </summary>
internal sealed class EfaConfigurationConfiguration : IEntityTypeConfiguration<EfaConfiguration>
{
    public void Configure(EntityTypeBuilder<EfaConfiguration> builder)
    {
        // Primary key
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.Year)
            .IsRequired();

        builder.Property(e => e.EfaRate)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedBy)
            .IsRequired();

        // Indexes
        builder.HasIndex(e => e.Year)
            .IsUnique()
            .HasDatabaseName("IX_EfaConfigurations_Year");

        // Table name (snake_case will be applied by EFCore.NamingConventions)
        builder.ToTable("efa_configurations");
    }
}
