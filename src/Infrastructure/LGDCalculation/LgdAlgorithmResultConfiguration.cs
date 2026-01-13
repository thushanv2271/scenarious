using Domain.LGDCalculation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.LGDCalculation;

/// <summary>
/// Entity Framework configuration for LgdAlgorithmResult entity
/// Uses JSONB column type for optimized PostgreSQL storage
/// </summary>
internal sealed class LgdAlgorithmResultConfiguration : IEntityTypeConfiguration<LgdAlgorithmResult>
{
    public void Configure(EntityTypeBuilder<LgdAlgorithmResult> builder)
    {
        builder.ToTable("lgd_algorithm_results");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        // Configure JSONB column for optimized storage and querying
        builder.Property(x => x.LgdAlgorithmResultData)
            .IsRequired()
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        builder.Property(x => x.UpdatedBy);

        // Index on CreatedAt for efficient retrieval of latest results
        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("ix_lgd_algorithm_results_created_at")
            .IsDescending();
    }
}