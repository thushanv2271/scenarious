namespace Application.Models;

/// <summary>
/// Represents a date passed due bucket configuration
/// </summary>
public sealed record DatePassedDueBucket(
    string Id,
    string RangeStart,
    string RangeEnd,
    string BucketLabel,
    string StageMapping,
    string RangeEndError,
    bool HasBeenTouched)
{
    /// <summary>
    /// Gets the range start as integer
    /// </summary>
    public int RangeStartInt => int.TryParse(RangeStart, out int value) ? value : 0;

    /// <summary>
    /// Gets the range end as integer
    /// </summary>
    public int RangeEndInt => int.TryParse(RangeEnd, out int value) ? value : int.MaxValue;

    /// <summary>
    /// Determines if the given days past due falls within this bucket's range
    /// </summary>
    /// <param name="daysPastDue">Days past due to check</param>
    /// <returns>True if the value falls within the range</returns>
    public bool IsInRange(int daysPastDue)
    {
        return daysPastDue >= RangeStartInt && daysPastDue <= RangeEndInt;
    }
}
