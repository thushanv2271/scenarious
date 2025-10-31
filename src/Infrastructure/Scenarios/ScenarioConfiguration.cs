using Domain.Files;
using Domain.Scenarios;
using Domain.Segments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Scenarios;

internal sealed class ScenarioConfiguration : IEntityTypeConfiguration<Scenario>
{
    public void Configure(EntityTypeBuilder<Scenario> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.SegmentId)
            .IsRequired();

        builder.Property(s => s.ScenarioName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Probability)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(s => s.ContractualCashFlowsEnabled)
            .IsRequired();

        builder.Property(s => s.LastQuarterCashFlowsEnabled)
            .IsRequired();

        builder.Property(s => s.OtherCashFlowsEnabled)
            .IsRequired();

        builder.Property(s => s.CollateralValueEnabled)
            .IsRequired();

        builder.Property(s => s.UploadedFileId)
            .IsRequired(false);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        builder.HasOne<Segment>()
            .WithMany()
            .HasForeignKey(s => s.SegmentId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_scenarios_segments_segment_id");

        builder.HasOne<UploadedFile>()
            .WithMany()
            .HasForeignKey(s => s.UploadedFileId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_scenarios_uploaded_files_uploaded_file_id");

        builder.HasIndex(s => s.SegmentId)
            .HasDatabaseName("ix_scenarios_segment_id");

        builder.HasIndex(s => new { s.SegmentId, s.ScenarioName })
            .IsUnique()
            .HasDatabaseName("ix_scenarios_segment_id_scenario_name");

        builder.HasIndex(s => s.UploadedFileId)
            .HasDatabaseName("ix_scenarios_uploaded_file_id");

        builder.ToTable("scenarios");
    }
}
