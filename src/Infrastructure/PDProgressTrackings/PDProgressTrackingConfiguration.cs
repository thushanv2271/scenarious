using Domain.PDProgressTrackings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.PDProgressTrackings;

internal sealed class PDProgressTrackingConfiguration : IEntityTypeConfiguration<PDProgressTracking>
{
    public void Configure(EntityTypeBuilder<PDProgressTracking> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.SessionId)
            .IsRequired();

        builder.Property(p => p.StepName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.StepOrder)
            .IsRequired();

        builder.Property(p => p.SubTaskName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.SubTaskOrder)
            .IsRequired();

        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.Message)
            .HasMaxLength(1000);

        builder.Property(p => p.StartedAt)
            .IsRequired(false);

        builder.Property(p => p.CompletedAt)
            .IsRequired(false);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.CreatedBy)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(p => p.UpdatedAt)
            .IsRequired(false);

        builder.Property(p => p.UpdatedBy)
            .HasMaxLength(256);

        // Indexes
        builder.HasIndex(p => p.SessionId)
            .HasDatabaseName("ix_pd_progress_tracking_session_id");

        builder.HasIndex(p => new { p.SessionId, p.StepOrder, p.SubTaskOrder })
            .HasDatabaseName("ix_pd_progress_tracking_session_step_subtask");

        builder.HasIndex(p => p.Status)
            .HasDatabaseName("ix_pd_progress_tracking_status");

        builder.HasIndex(p => p.IsActive)
            .HasDatabaseName("ix_pd_progress_tracking_is_active");

        // Table name
        builder.ToTable("pd_progress_tracking");
    }
}
