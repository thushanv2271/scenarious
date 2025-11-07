namespace Application.Industries.GetAll;

/// <summary>
/// Response containing industry information.
/// </summary>
public sealed record IndustryResponse(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    DateTime UpdatedAt
);