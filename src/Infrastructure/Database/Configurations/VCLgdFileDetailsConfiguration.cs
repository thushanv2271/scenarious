using Domain.LGDCalculation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Configurations;

/// <summary>
/// Entity configuration for VCLgdFileDetails
/// </summary>
internal sealed class VCLgdFileDetailsConfiguration : IEntityTypeConfiguration<VCLgdFileDetails>
{
    public void Configure(EntityTypeBuilder<VCLgdFileDetails> builder)
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

        // One-to-many relationship with VCLgdDetails
        builder.HasMany(x => x.VCLgdDetails)
            .WithOne(x => x.VCLgdFileDetails)
            .HasForeignKey(x => x.VCLgdFileDetailsId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("vc_lgd_file_details");
    }
}