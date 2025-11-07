namespace Application.Industries.Create;

/// <summary>
/// Response for the create industry command.
/// </summary>
public sealed record CreateIndustryResponse(
    bool Success,
    int TotalProcessed,
    int CreatedCount,
    int SkippedCount,
    IReadOnlyList<CreatedIndustry> CreatedIndustries,
    IReadOnlyList<string> SkippedNames
);

/// <summary>
/// Represents a successfully created industry.
/// </summary>
public sealed record CreatedIndustry(
    Guid Id,
    string Name,
    DateTime CreatedAt
);