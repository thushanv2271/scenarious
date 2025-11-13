namespace Domain.PDCalculation;

/// <summary>
/// Represents age bucket categories for Days Past Due classification
/// </summary>
public enum AgeBucket
{
    /// <summary>
    /// Current (Days Past Due < 1)
    /// </summary>
    Current = 1,

    /// <summary>
    /// 1-30 Days Past Due
    /// </summary>
    Days1To30 = 2,

    /// <summary>
    /// 31-60 Days Past Due
    /// </summary>
    Days31To60 = 3,

    /// <summary>
    /// 61-90 Days Past Due
    /// </summary>
    Days61To90 = 4,

    /// <summary>
    /// Above 90 Days Past Due
    /// </summary>
    Above90Days = 5
}
