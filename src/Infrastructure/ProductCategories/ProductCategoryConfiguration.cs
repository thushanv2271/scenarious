using Domain.ProductCategories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.ProductCategories;

internal sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Type)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired();

        builder.HasIndex(p => new { p.Type, p.Name })
            .IsUnique()
            .HasDatabaseName("ix_product_categories_type_name");

        // Configure one-to-many relationship with Segments
        builder.HasMany(pc => pc.Segments)
            .WithOne(s => s.ProductCategory)
            .HasForeignKey(s => s.ProductCategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("product_categories");
    }
}
