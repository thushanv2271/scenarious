using System.Globalization;

namespace Infrastructure.LgdCalculationSteps.Helpers;

/// <summary>
/// Calculator for determining the number of years from NPL (Non-Performing Loan) date to closure date.
/// Implements Excel formula: =IF([Facility Number]="","",MAX(ROUND(([Closure Date]-[First NPL Date])/365,0),0))
/// </summary>
internal static class LgdYearsToClosureCalculator
{
    /// <summary>
    /// Calculates the number of years from NPL date to closure date.
    /// Implements Excel formula: MAX(ROUND(([Closure Date]-[First NPL Date])/365,0),0)
    /// </summary>
    /// <param name="closureDate">The closure date of the facility</param>
    /// <param name="firstNplDate">The first NPL (Non-Performing Loan) date</param>
    /// <returns>Number of years from NPL date to closure date, minimum 0</returns>
    public static decimal CalculateYearsToClosureFromNpl(DateTime? closureDate, DateTime? firstNplDate)
    {
        // Handle null dates - return 0 if either date is missing
        if (closureDate is null || firstNplDate is null)
        {
            return 0m;
        }

        // Handle invalid date order - if closure date is before NPL date, return 0
        if (closureDate < firstNplDate)
        {
            return 0m;
        }

        try
        {
            // Calculate difference in days
            TimeSpan dateDifference = closureDate.Value - firstNplDate.Value;
            double totalDays = dateDifference.TotalDays;

            // Convert to years (divide by 365)
            decimal yearsDecimal = (decimal)(totalDays / 365.0);

            // Round to nearest integer
            decimal roundedYears = Math.Round(yearsDecimal, 0, MidpointRounding.AwayFromZero);

            // Ensure minimum value is 0 (MAX function in Excel)
            return Math.Max(roundedYears, 0m);
        }
        catch (Exception)
        {
            // Return 0 for any calculation errors
            return 0m;
        }
    }

    /// <summary>
    /// Calculates years to closure for multiple facilities with Excel IF logic.
    /// Implements: IF([Facility Number]="","",MAX(ROUND(([Closure Date]-[First NPL Date])/365,0),0))
    /// </summary>
    /// <param name="facilityData">Dictionary mapping facility numbers to their closure and NPL dates</param>
    /// <returns>Dictionary mapping facility numbers to years from NPL to closure</returns>
    /// <exception cref="ArgumentNullException">Thrown when facilityData is null</exception>
    public static Dictionary<string, decimal> CalculateYearsToClosureByFacility(
        Dictionary<string, (DateTime? closureDate, DateTime? firstNplDate)> facilityData)
    {
        ArgumentNullException.ThrowIfNull(facilityData);

        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, (DateTime? closureDate, DateTime? firstNplDate)> kvp in facilityData)
        {
            string facilityNumber = kvp.Key;
            (DateTime? closureDate, DateTime? firstNplDate) dates = kvp.Value;

            // Excel IF logic: IF([Facility Number]="","", ...)
            // In our case, if facility number is empty or null, we skip it (equivalent to returning empty)
            if (string.IsNullOrWhiteSpace(facilityNumber))
            {
                continue; // Skip empty facility numbers
            }

            // Calculate years to closure for this facility
            decimal yearsToClosureFromNpl = CalculateYearsToClosureFromNpl(dates.closureDate, dates.firstNplDate);
            result[facilityNumber] = yearsToClosureFromNpl;
        }

        return result;
    }

    /// <summary>
    /// Calculates portfolio-level statistics for years to closure from NPL date.
    /// </summary>
    /// <param name="facilityYearsToClosureFromNpl">Dictionary of facility years to closure values</param>
    /// <returns>Statistics for the portfolio</returns>
    /// <exception cref="ArgumentNullException">Thrown when input dictionary is null</exception>
    public static YearsToClosureStatistics CalculateYearsToClosureStatistics(
        Dictionary<string, decimal> facilityYearsToClosureFromNpl)
    {
        ArgumentNullException.ThrowIfNull(facilityYearsToClosureFromNpl);

        if (facilityYearsToClosureFromNpl.Count == 0)
        {
            return new YearsToClosureStatistics
            {
                TotalFacilities = 0,
                MinYears = 0m,
                MaxYears = 0m,
                AverageYears = 0m,
                FacilitiesWithZeroYears = 0,
                FacilitiesWithPositiveYears = 0
            };
        }

        var values = facilityYearsToClosureFromNpl.Values.ToList();

        return new YearsToClosureStatistics
        {
            TotalFacilities = values.Count,
            MinYears = values.Min(),
            MaxYears = values.Max(),
            AverageYears = Math.Round(values.Average(), 2),
            FacilitiesWithZeroYears = values.Count(y => y == 0m),
            FacilitiesWithPositiveYears = values.Count(y => y > 0m)
        };
    }

    /// <summary>
    /// Validates if the calculated years to closure value is reasonable.
    /// </summary>
    /// <param name="yearsToClosureFromNpl">Years to closure value to validate</param>
    /// <returns>Validation result with any warnings</returns>
    public static YearsToClosureValidation ValidateYearsToClosureFromNpl(decimal yearsToClosureFromNpl)
    {
        var warnings = new List<string>();

        // Check for unusually long periods
        if (yearsToClosureFromNpl > 20)
        {
            warnings.Add($"Years to closure ({yearsToClosureFromNpl}) is unusually high (>20 years)");
        }

        // Check for negative values (should not happen with our logic, but good to validate)
        if (yearsToClosureFromNpl < 0)
        {
            warnings.Add($"Years to closure ({yearsToClosureFromNpl}) is negative");
        }

        return new YearsToClosureValidation
        {
            IsValid = warnings.Count == 0,
            Warnings = warnings,
            Value = yearsToClosureFromNpl
        };
    }

    /// <summary>
    /// Formats years to closure for display purposes.
    /// </summary>
    /// <param name="yearsToClosureFromNpl">Years to closure value</param>
    /// <param name="format">Format string (default: "F1" for 1 decimal place)</param>
    /// <returns>Formatted string representation</returns>
    public static string FormatYearsToClosureFromNpl(decimal yearsToClosureFromNpl, string format = "F1")
    {
        if (yearsToClosureFromNpl == 0m)
        {
            return "0.0 years";
        }

        if (yearsToClosureFromNpl == 1m)
        {
            return "1.0 year";
        }

        return $"{yearsToClosureFromNpl.ToString(format, CultureInfo.InvariantCulture)} years";
    }

    /// <summary>
    /// Gets facilities categorized by years to closure ranges.
    /// </summary>
    /// <param name="facilityYearsToClosureFromNpl">Dictionary of facility years to closure values</param>
    /// <returns>Categorized facilities by years to closure ranges</returns>
    /// <exception cref="ArgumentNullException">Thrown when input dictionary is null</exception>
    public static YearsToClosureCategorization CategorizeByYearsToClosureFromNpl(
        Dictionary<string, decimal> facilityYearsToClosureFromNpl)
    {
        ArgumentNullException.ThrowIfNull(facilityYearsToClosureFromNpl);

        var zeroYears = new List<string>();
        var oneToThreeYears = new List<string>();
        var threeToFiveYears = new List<string>();
        var fiveToTenYears = new List<string>();
        var moreThanTenYears = new List<string>();

        foreach (KeyValuePair<string, decimal> kvp in facilityYearsToClosureFromNpl)
        {
            string facilityNumber = kvp.Key;
            decimal years = kvp.Value;

            switch (years)
            {
                case 0m:
                    zeroYears.Add(facilityNumber);
                    break;
                case > 0m and <= 3m:
                    oneToThreeYears.Add(facilityNumber);
                    break;
                case > 3m and <= 5m:
                    threeToFiveYears.Add(facilityNumber);
                    break;
                case > 5m and <= 10m:
                    fiveToTenYears.Add(facilityNumber);
                    break;
                case > 10m:
                    moreThanTenYears.Add(facilityNumber);
                    break;
            }
        }

        return new YearsToClosureCategorization
        {
            ZeroYears = zeroYears,
            OneToThreeYears = oneToThreeYears,
            ThreeToFiveYears = threeToFiveYears,
            FiveToTenYears = fiveToTenYears,
            MoreThanTenYears = moreThanTenYears
        };
    }
}

/// <summary>
/// Statistics for years to closure from NPL date calculations.
/// </summary>
internal sealed record YearsToClosureStatistics
{
    /// <summary>
    /// Total number of facilities analyzed.
    /// </summary>
    public required int TotalFacilities { get; init; }

    /// <summary>
    /// Minimum years to closure across all facilities.
    /// </summary>
    public required decimal MinYears { get; init; }

    /// <summary>
    /// Maximum years to closure across all facilities.
    /// </summary>
    public required decimal MaxYears { get; init; }

    /// <summary>
    /// Average years to closure across all facilities.
    /// </summary>
    public required decimal AverageYears { get; init; }

    /// <summary>
    /// Number of facilities with zero years to closure.
    /// </summary>
    public required int FacilitiesWithZeroYears { get; init; }

    /// <summary>
    /// Number of facilities with positive years to closure.
    /// </summary>
    public required int FacilitiesWithPositiveYears { get; init; }
}

/// <summary>
/// Validation result for years to closure calculations.
/// </summary>
internal sealed record YearsToClosureValidation
{
    /// <summary>
    /// Indicates if the value passes validation checks.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// List of validation warnings, if any.
    /// </summary>
    public required List<string> Warnings { get; init; }

    /// <summary>
    /// The validated years to closure value.
    /// </summary>
    public required decimal Value { get; init; }
}

/// <summary>
/// Categorization of facilities by years to closure from NPL date ranges.
/// </summary>
internal sealed record YearsToClosureCategorization
{
    /// <summary>
    /// Facilities with zero years to closure (closed on same date as NPL or before).
    /// </summary>
    public required List<string> ZeroYears { get; init; }

    /// <summary>
    /// Facilities with 1-3 years to closure.
    /// </summary>
    public required List<string> OneToThreeYears { get; init; }

    /// <summary>
    /// Facilities with 3-5 years to closure.
    /// </summary>
    public required List<string> ThreeToFiveYears { get; init; }

    /// <summary>
    /// Facilities with 5-10 years to closure.
    /// </summary>
    public required List<string> FiveToTenYears { get; init; }

    /// <summary>
    /// Facilities with more than 10 years to closure.
    /// </summary>
    public required List<string> MoreThanTenYears { get; init; }
}