namespace Application.Organizations;

public sealed record OrganizationResponse(
    Guid Id,
    string Name,
    string Code,
    string Email,
    string ContactNumber,
    string Address,
    bool IsActive,
    DateOnly? FinancialYearEnd,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
