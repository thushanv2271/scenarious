using Domain.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class FileValidationResultConfiguration : IEntityTypeConfiguration<FileValidationResult>
{
    public void Configure(EntityTypeBuilder<FileValidationResult> builder)
    {
        builder.ToTable("file_validation_results");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Filename)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.TotalRows)
            .IsRequired();

        builder.Property(x => x.TotalErrors)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.TimePeriod)
            .HasMaxLength(50);

        builder.Property(x => x.CollectiveImpairmentType)
            .HasMaxLength(10);

        builder.Property(x => x.CreatedOnUtc)
            .IsRequired();

        builder.Property(x => x.ModifiedOnUtc);

        // Create index for efficient filtering by TimePeriod and CollectiveImpairmentType
        builder.HasIndex(x => new { x.TimePeriod, x.CollectiveImpairmentType })
            .HasDatabaseName("ix_file_validation_results_time_period_type");

        // Keep existing indexes for backward compatibility
        builder.HasIndex(x => x.Filename)
            .HasDatabaseName("ix_file_validation_results_filename");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("ix_file_validation_results_status");
    }
}
