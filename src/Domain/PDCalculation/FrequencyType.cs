namespace Domain.PDCalculation;

/// <summary>
/// Represents the frequency type of PD files
/// </summary>
public enum FrequencyType
{
    /// <summary>
    /// Yearly frequency (PD_year_part.csv)
    /// </summary>
    Yearly = 1,

    /// <summary>
    /// Monthly frequency (PD_year-month_part.csv)
    /// </summary>
    Monthly = 2,

    /// <summary>
    /// Quarterly frequency (PD_yearQuarter_part.csv)
    /// </summary>
    Quarterly = 3
}
