using Domain.PDCalculation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Configurations;

/// <summary>
/// Entity configuration for LoanDetails
/// </summary>
internal sealed class LoanDetailsConfiguration : IEntityTypeConfiguration<LoanDetails>
{
    public void Configure(EntityTypeBuilder<LoanDetails> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.FileDetailsId)
            .IsRequired();

        builder.Property(x => x.CustomerNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.FacilityNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Branch)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ProductCategory)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Segment)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Industry)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.EarningType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Nature)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.GrantDate)
            .IsRequired();

        builder.Property(x => x.MaturityDate)
            .IsRequired();

        builder.Property(x => x.InterestRate)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.InstallmentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.DaysPastDue)
            .IsRequired();

        builder.Property(x => x.Limit)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.TotalOS)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.UndisbursedAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.InterestInSuspense)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.CollateralType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CollateralValue)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Rescheduled)
            .IsRequired();

        builder.Property(x => x.Restructured)
            .IsRequired();

        builder.Property(x => x.NoOfTimesRestructured)
            .IsRequired();

        builder.Property(x => x.UpgradedToDelinquencyBucket)
            .IsRequired();

        builder.Property(x => x.IndividuallyImpaired)
            .IsRequired();

        builder.Property(x => x.BucketingInIndividualAssessment)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Period)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.RemainingMaturityYears)
            .IsRequired();

        builder.Property(x => x.BucketLabel)
            .HasColumnName("bucket_label")
            .HasMaxLength(100)
            .IsRequired();

        // Foreign key relationship
        builder.HasOne(x => x.FileDetails)
            .WithMany(x => x.LoanDetails)
            .HasForeignKey(x => x.FileDetailsId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("loan_details");
    }
}
