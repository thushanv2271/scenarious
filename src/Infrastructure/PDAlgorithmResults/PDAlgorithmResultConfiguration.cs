using Domain.PDAlgorithmResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.PDAlgorithmResults;

/// <summary>
/// Entity Framework configuration for PDAlgorithmResult entity
/// Uses JSONB column type for optimized PostgreSQL storage
/// </summary>
internal sealed class PDAlgorithmResultConfiguration : IEntityTypeConfiguration<PDAlgorithmResult>
{
    public void Configure(EntityTypeBuilder<PDAlgorithmResult> builder)
    {
        builder.ToTable("pd_algorithm_results");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        // Configure JSONB column for optimized storage and querying
        builder.Property(x => x.PdAlgorithmResultData)
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
            .HasDatabaseName("ix_pd_algorithm_results_created_at")
            .IsDescending();
    }
}
