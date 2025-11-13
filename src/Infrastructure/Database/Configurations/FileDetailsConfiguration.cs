using Domain.PDCalculation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Configurations;

/// <summary>
/// Entity configuration for FileDetails
/// </summary>
internal sealed class FileDetailsConfiguration : IEntityTypeConfiguration<FileDetails>
{
    public void Configure(EntityTypeBuilder<FileDetails> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FileName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Frequency)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Part)
            .IsRequired();

        builder.Property(x => x.QuarterEndedDate)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(200)
            .IsRequired();

        // One-to-many relationship with LoanDetails
        builder.HasMany(x => x.LoanDetails)
            .WithOne(x => x.FileDetails)
            .HasForeignKey(x => x.FileDetailsId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("file_details");
    }
}
