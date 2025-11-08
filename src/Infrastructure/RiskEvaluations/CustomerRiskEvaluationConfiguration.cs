using Domain.RiskEvaluations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.RiskEvaluations;

// EF Core configuration for CustomerRiskEvaluation entity
internal sealed class CustomerRiskEvaluationConfiguration
    : IEntityTypeConfiguration<CustomerRiskEvaluation>
{
    public void Configure(EntityTypeBuilder<CustomerRiskEvaluation> builder)
    {
        builder.ToTable("customer_risk_evaluations"); // Table name

        builder.HasKey(c => c.EvaluationId); // Primary key

        builder.Property(c => c.EvaluationId)
            .HasColumnName("evaluation_id")
            .ValueGeneratedNever(); // Provided by application

        builder.Property(c => c.CustomerNumber)
            .HasColumnName("customer_number")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.EvaluationDate)
            .HasColumnName("evaluation_date")
            .IsRequired();

        builder.Property(c => c.EvaluatedBy)
            .HasColumnName("evaluated_by")
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at");

        // Relationship: one evaluation → many indicator entries
        builder.HasMany(c => c.IndicatorEvaluations)
            .WithOne(i => i.Evaluation)
            .HasForeignKey(i => i.EvaluationId)
            .OnDelete(DeleteBehavior.Cascade); // Delete children when parent deleted

        // Index on CustomerNumber
        builder.HasIndex(c => c.CustomerNumber)
            .HasDatabaseName("ix_customer_risk_evaluations_customer_number");

        // Index for customer+date combination
        builder.HasIndex(c => new { c.CustomerNumber, c.EvaluationDate })
            .HasDatabaseName("ix_customer_risk_evaluations_customer_date");
    }
}
