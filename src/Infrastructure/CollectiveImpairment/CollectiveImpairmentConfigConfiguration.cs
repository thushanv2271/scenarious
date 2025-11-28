using Domain.CollectiveImpairment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.CollectiveImpairment;

internal sealed class CollectiveImpairmentConfigConfiguration : IEntityTypeConfiguration<CollectiveImpairmentConfig>
{
    public void Configure(EntityTypeBuilder<CollectiveImpairmentConfig> builder)
    {
        builder.ToTable("collective_impairment_configs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Parameter)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ConfigJson)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .IsRequired();

        builder.Property(x => x.CreatedDate)
            .IsRequired();

        builder.Property(x => x.UpdatedBy)
            .IsRequired(false);

        builder.Property(x => x.UpdatedDate)
            .IsRequired(false);

        builder.HasIndex(x => x.Parameter);
    }
}
