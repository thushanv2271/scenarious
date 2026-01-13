using Application.Abstractions.Data;

namespace Saral.FileProcessor.Core.Services;

public sealed class ValidationConfigurationService : IValidationConfigurationService
{
    private readonly IApplicationDbContext _dbContext;

    public ValidationConfigurationService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public IDataValidator ConfigureValidation(ReadOnlySpan<string> columnNames)
    {
        var validator = new DataValidator();

        // Build a case-insensitive set once to avoid repeated O(n) scans
        HashSet<string> headers = ToHeaderSet(columnNames);

        // Load reference data from database (cached for this validation session)
        HashSet<string> validSegments = LoadValidSegments();
        HashSet<string> validProductCategories = LoadValidProductCategories();

        // Configure rules
        ConfigureIdentityColumns(validator, headers);
        ConfigureProductAndBusinessColumns(validator, headers, validSegments, validProductCategories);
        ConfigureFinancialColumns(validator, headers);
        ConfigureCollateralAndStatusColumns(validator, headers);

        return validator;
    }

    private HashSet<string> LoadValidSegments()
    {
        return _dbContext.Segments
            .Select(s => s.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private HashSet<string> LoadValidProductCategories()
    {
        return _dbContext.ProductCategories
            .Select(pc => pc.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string> ToHeaderSet(ReadOnlySpan<string> columnNames)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string name in columnNames)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                set.Add(name.Trim());
            }
        }

        return set;
    }

    private static void ConfigureIdentityColumns(DataValidator v, ISet<string> headers)
    {
        v.AddIfPresent(headers, Columns.CustomerNumber,
            () => new NotEmptyValidator(Columns.CustomerNumber));

        v.AddIfPresent(headers, Columns.FacilityNumber,
            () => new NotEmptyValidator(Columns.FacilityNumber),
            () => new UniqueValidator(Columns.FacilityNumber));

        v.AddIfPresent(headers, Columns.Branch,
            () => new NotEmptyValidator(Columns.Branch));
    }

    private static void ConfigureProductAndBusinessColumns(
        DataValidator v,
        ISet<string> headers,
        HashSet<string> validSegments,
        HashSet<string> validProductCategories)
    {
        v.AddIfPresent(headers, Columns.ProductCategory,
            () => new NotEmptyValidator(Columns.ProductCategory),
            () => new ReferenceDataValidator(Columns.ProductCategory, validProductCategories));

        v.AddIfPresent(headers, Columns.Segment,
            () => new NotEmptyValidator(Columns.Segment),
            () => new ReferenceDataValidator(Columns.Segment, validSegments));

        v.AddIfPresent(headers, Columns.Industry,
            () => new WarningValidator(Columns.Industry,
                "Industry field is optional but recommended for better analysis"));

        v.AddIfPresent(headers, Columns.Nature,
            () => new NotEmptyValidator(Columns.Nature));

        // Maturity date required unless Nature == Revolving (allow empty when revolving)
        v.AddIfPresent(headers, Columns.MaturityDateExpiryDate,
            () => new ConditionalNotEmptyValidator(
                Columns.MaturityDateExpiryDate,
                Columns.Nature,
                Constants.Revolving,
                allowEmptyWhenConditionMet: true));
    }

    private static void ConfigureFinancialColumns(DataValidator v, ISet<string> headers)
    {
        v.AddIfPresent(headers, Columns.InterestRate,
            () => new NotEmptyValidator(Columns.InterestRate),
            () => new NumericRangeValidator(Columns.InterestRate, 0, double.MaxValue));

        // Required unless Nature == Revolving
        v.AddIfPresent(headers, Columns.InstallmentType,
            () => new ConditionalNotEmptyValidator(
                Columns.InstallmentType,
                Columns.Nature,
                Constants.Revolving,
                allowEmptyWhenConditionMet: true));

        v.AddIfPresent(headers, Columns.DaysPastDue,
            () => new NotEmptyValidator(Columns.DaysPastDue),
            () => new NumericRangeValidator(Columns.DaysPastDue, 0, double.MaxValue));

        v.AddIfPresent(headers, Columns.Limit,
            () => new NumericRangeValidator(Columns.Limit, 0, double.MaxValue));

        v.AddIfPresent(headers, Columns.TotalOS,
            () => new DependentValidator(Columns.TotalOS, Columns.Nature, "Required"));

        v.AddIfPresent(headers, Columns.UndisbursedAmount,
            () => new NumericRangeValidator(Columns.UndisbursedAmount, 0, double.MaxValue));
    }

    private static void ConfigureCollateralAndStatusColumns(DataValidator v, ISet<string> headers)
    {
        v.AddIfPresent(headers, Columns.CollateralValue,
            () => new NumericRangeValidator(Columns.CollateralValue, 0, double.MaxValue));

        v.AddIfPresent(headers, Columns.NoOfTimesRestructured,
            () => new NumericRangeValidator(Columns.NoOfTimesRestructured, 0, double.MaxValue));

        // v.AddIfPresent(headers, Columns.UpgradedToDelinquencyBucket,
        //     () => new BooleanValidator(Columns.UpgradedToDelinquencyBucket));

        v.AddIfPresent(headers, Columns.IndividuallyImpaired,
            () => new BooleanValidator(Columns.IndividuallyImpaired));

        v.AddIfPresent(headers, Columns.BucketingInIndividualAssessment,
            () => new NumericRangeValidator(Columns.BucketingInIndividualAssessment, 0, double.MaxValue));
    }

    // Centralized column name constants (no magic strings across the codebase)
    private static class Columns
    {
        public const string CustomerNumber = "Customer Number";
        public const string FacilityNumber = "Facility number";
        public const string Branch = "Branch";

        public const string ProductCategory = "Product category";
        public const string Segment = "Segment";
        public const string Industry = "Industry";
        public const string Nature = "Nature";
        public const string MaturityDateExpiryDate = "Maturity date/ Expiry Date";

        public const string InterestRate = "Interest Rate";
        public const string InstallmentType = "Installment Type (Monthly/ Quarterly/ Weekly/ Daily/ Annually/ Bullet)";
        public const string DaysPastDue = "Days Past Due";
        public const string Limit = "Limit";
        public const string TotalOS = "Total OS";
        public const string UndisbursedAmount = "Undisbursed Amount";

        public const string CollateralValue = "Collateral Value";
        public const string NoOfTimesRestructured = "No. of Times Restructured";
        //public const string UpgradedToDelinquencyBucket = "Upgraded to delinquency bucket (Yes/No)";
        public const string IndividuallyImpaired = "Individually Impaired (Yes/No)";
        public const string BucketingInIndividualAssessment = "Bucketing in Individual Assessment";
    }

    private static class Constants
    {
        public const string Revolving = "Revolving";
    }
}

internal static class DataValidatorExtensions
{
    /// <summary>
    /// Adds one or more validators only if the column header is present.
    /// Keeps configuration code compact and consistent.
    /// </summary>
    public static void AddIfPresent(
        this DataValidator validator,
        ISet<string> headers,
        string columnName,
        params Func<IColumnValidator>[] factories)
    {
        if (!headers.Contains(columnName))
        {
            return;
        }

        foreach (Func<IColumnValidator> factory in factories)
        {
            validator.AddValidator(factory());
        }
    }
}
