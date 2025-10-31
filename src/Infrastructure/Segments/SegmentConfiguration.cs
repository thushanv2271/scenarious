using Domain.Segments;
using Domain.ProductCategories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Segments;

internal sealed class SegmentConfiguration : IEntityTypeConfiguration<Segment>
{
    public void Configure(EntityTypeBuilder<Segment> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.ProductCategoryId)
            .IsRequired();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        builder.HasOne<ProductCategory>()
            .WithMany()
            .HasForeignKey(s => s.ProductCategoryId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_segments_product_categories_product_category_id");

        builder.HasIndex(s => s.ProductCategoryId)
            .HasDatabaseName("ix_segments_product_category_id");

        builder.HasIndex(s => new { s.ProductCategoryId, s.Name })
            .IsUnique()
            .HasDatabaseName("ix_segments_product_category_id_name");

        builder.ToTable("segments");
    }
}
