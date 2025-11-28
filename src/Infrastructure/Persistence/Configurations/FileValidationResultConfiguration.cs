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

        builder.Property(x => x.CreatedOnUtc)
            .IsRequired();

        builder.Property(x => x.ModifiedOnUtc);
    }
}