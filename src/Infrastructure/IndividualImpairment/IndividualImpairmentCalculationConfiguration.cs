using Domain.IndividualImpairment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.IndividualImpairment;

internal sealed class IndividualImpairmentCalculationConfiguration
    : IEntityTypeConfiguration<IndividualImpairmentCalculation>
{
    public void Configure(EntityTypeBuilder<IndividualImpairmentCalculation> builder)
    {
        builder.ToTable("individual_impairment_calculations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FacilityNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.CustomerNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.CalculationDate)
            .IsRequired();

        builder.Property(x => x.InterestRate)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(x => x.AmortizedCost)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.SumOfPVOfCashFlows)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.ImpairmentAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.ImpairmentPercentage)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(x => x.ScenarioDetailsJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(x => x.CalculatedBy)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(x => x.CustomerNumber)
            .HasDatabaseName("ix_individual_impairment_calculations_customer_number");

        builder.HasIndex(x => x.FacilityNumber)
            .HasDatabaseName("ix_individual_impairment_calculations_facility_number");

        builder.HasIndex(x => x.CalculationDate)
            .HasDatabaseName("ix_individual_impairment_calculations_calculation_date");

        builder.HasIndex(x => new { x.CustomerNumber, x.CalculationDate })
            .HasDatabaseName("ix_individual_impairment_calculations_customer_date");
    }
}
