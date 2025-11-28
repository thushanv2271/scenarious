using Domain.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Infrastructure.Configurations;

internal sealed class AgeBucketConfigurationConfiguration : IEntityTypeConfiguration<AgeBucketConfiguration>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public void Configure(EntityTypeBuilder<AgeBucketConfiguration> builder)
    {
        builder.ToTable("age_bucket_configurations");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        // Store all configuration data as a single JSON column
        builder.Property(x => x.ConfigurationData)
            .HasColumnName("ConfigurationData")
            .HasColumnType("jsonb")
            .HasConversion(
                data => JsonSerializer.Serialize(data, JsonOptions),
                json => JsonSerializer.Deserialize<AgeBucketConfigurationData>(json, JsonOptions) ?? new AgeBucketConfigurationData())
            .IsRequired();

        builder.Property(x => x.CreatedOnUtc)
            .IsRequired();

        builder.Property(x => x.ModifiedOnUtc);
    }
}