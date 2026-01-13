using Domain.LGDCalculation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Configurations;

/// <summary>
/// Entity configuration for LgdFileDetails
/// </summary>
internal sealed class LgdFileDetailsConfiguration : IEntityTypeConfiguration<LgdFileDetails>
{
    public void Configure(EntityTypeBuilder<LgdFileDetails> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FileName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Year)
            .IsRequired();

        builder.Property(x => x.Part)
            .IsRequired();

        builder.Property(x => x.Period)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(200)
            .IsRequired();

        // One-to-many relationship with LgdDetails
        builder.HasMany(x => x.LgdDetails)
            .WithOne(x => x.LgdFileDetails)
            .HasForeignKey(x => x.LgdFileDetailsId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("lgd_file_details");
    }
}