using Domain.RiskEvaluations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.RiskEvaluations;

// EF Core configuration for RiskIndicator entity
internal sealed class RiskIndicatorConfiguration : IEntityTypeConfiguration<RiskIndicator>
{
    public void Configure(EntityTypeBuilder<RiskIndicator> builder)
    {
        builder.ToTable("risk_indicators"); // Table name

        builder.HasKey(r => r.IndicatorId); // Primary key

        builder.Property(r => r.IndicatorId)
            .HasColumnName("indicator_id")
            .ValueGeneratedNever(); // GUIDs generated client-side

        builder.Property(r => r.Category)
            .HasColumnName("category")
            .HasConversion<string>() // Store enum as string
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(r => r.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(r => r.PossibleValues)
            .HasColumnName("possible_values")
            .HasMaxLength(100)
            .HasDefaultValue("Yes,No,N/A")
            .IsRequired(); // Stored as comma-separated list

        builder.Property(r => r.DisplayOrder)
            .HasColumnName("display_order")
            .IsRequired();

        builder.Property(r => r.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at");

        // Index for grouping & sorting by category + display order
        builder.HasIndex(r => new { r.Category, r.DisplayOrder })
            .HasDatabaseName("ix_risk_indicators_category_display_order");
    }
}
