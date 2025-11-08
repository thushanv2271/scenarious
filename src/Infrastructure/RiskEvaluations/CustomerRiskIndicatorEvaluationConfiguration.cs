using Domain.RiskEvaluations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.RiskEvaluations;

// EF Core configuration for CustomerRiskIndicatorEvaluation entity
internal sealed class CustomerRiskIndicatorEvaluationConfiguration
    : IEntityTypeConfiguration<CustomerRiskIndicatorEvaluation>
{
    public void Configure(EntityTypeBuilder<CustomerRiskIndicatorEvaluation> builder)
    {
        builder.ToTable("customer_risk_indicator_evaluations"); // Table name

        builder.HasKey(c => c.EvalDetailId); // Primary key

        builder.Property(c => c.EvalDetailId)
            .HasColumnName("eval_detail_id")
            .ValueGeneratedNever(); // Provided by application

        builder.Property(c => c.EvaluationId)
            .HasColumnName("evaluation_id")
            .IsRequired();

        builder.Property(c => c.IndicatorId)
            .HasColumnName("indicator_id")
            .IsRequired();

        builder.Property(c => c.Value)
            .HasColumnName("value")
            .HasMaxLength(10)
            .IsRequired(); // Yes / No / N/A

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at");

        // Relationship: detail belongs to an evaluation
        builder.HasOne(c => c.Evaluation)
            .WithMany(e => e.IndicatorEvaluations)
            .HasForeignKey(c => c.EvaluationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship: detail references an indicator
        builder.HasOne(c => c.Indicator)
            .WithMany()
            .HasForeignKey(c => c.IndicatorId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent deleting master indicator

        // Index on evaluation id
        builder.HasIndex(c => c.EvaluationId)
            .HasDatabaseName("ix_customer_risk_indicator_evaluations_evaluation_id");

        // Index on indicator id
        builder.HasIndex(c => c.IndicatorId)
            .HasDatabaseName("ix_customer_risk_indicator_evaluations_indicator_id");

        // Unique constraint per evaluation + indicator
        builder.HasIndex(c => new { c.EvaluationId, c.IndicatorId })
            .IsUnique()
            .HasDatabaseName("ix_customer_risk_indicator_evaluations_unique");
    }
}
