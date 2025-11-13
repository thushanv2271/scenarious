using Domain.FacilityCashFlowTypes;
using Domain.Scenarios;
using Domain.Segments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.FacilityCashFlowTypes;

/// <summary>
/// Entity Framework configuration for FacilityCashFlowType entity
/// </summary>
internal sealed class FacilityCashFlowTypeConfiguration
    : IEntityTypeConfiguration<FacilityCashFlowType>
{
    public void Configure(EntityTypeBuilder<FacilityCashFlowType> builder)
    {
        builder.ToTable("facility_cash_flow_types");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.FacilityNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(f => f.SegmentId)
            .IsRequired();

        builder.Property(f => f.ScenarioId)
            .IsRequired();

        builder.Property(f => f.CashFlowType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(f => f.Configuration)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(f => f.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(f => f.CreatedBy)
            .IsRequired();

        builder.Property(f => f.CreatedAt)
            .IsRequired();

        builder.Property(f => f.UpdatedAt)
            .IsRequired();

        // Foreign key relationships
        builder.HasOne<Segment>()
            .WithMany()
            .HasForeignKey(f => f.SegmentId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_facility_cash_flow_types_segments");

        builder.HasOne<Scenario>()
            .WithMany()
            .HasForeignKey(f => f.ScenarioId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_facility_cash_flow_types_scenarios");

        // Indexes
        builder.HasIndex(f => f.FacilityNumber)
            .HasDatabaseName("ix_facility_cash_flow_types_facility_number");

        builder.HasIndex(f => f.SegmentId)
            .HasDatabaseName("ix_facility_cash_flow_types_segment_id");

        builder.HasIndex(f => f.ScenarioId)
            .HasDatabaseName("ix_facility_cash_flow_types_scenario_id");

        builder.HasIndex(f => f.IsActive)
            .HasFilter("is_active = true")
            .HasDatabaseName("ix_facility_cash_flow_types_is_active");

        // Unique constraint for active records
        builder.HasIndex(f => new { f.FacilityNumber, f.ScenarioId, f.IsActive })
            .HasFilter("is_active = true")
            .IsUnique()
            .HasDatabaseName("uq_facility_scenario_active");
    }
}
