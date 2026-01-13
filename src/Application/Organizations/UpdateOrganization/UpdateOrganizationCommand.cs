using Application.Abstractions.Messaging;

namespace Application.Organizations.UpdateOrganization;

public sealed record UpdateOrganizationCommand(
    Guid Id,
    string Name,
    string Email,
    string ContactNumber,
    string Address,
    bool IsActive,
    DateOnly? FinancialYearEnd
) : ICommand;
