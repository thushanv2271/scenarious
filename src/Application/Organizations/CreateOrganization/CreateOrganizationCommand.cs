using Application.Abstractions.Messaging;

namespace Application.Organizations.CreateOrganization;

public sealed record CreateOrganizationCommand(
    string Name,
    string Code,
    string Email,
    string ContactNumber,
    string Address,
    bool IsActive = true,
    DateOnly? FinancialYearEnd = null
) : ICommand<Guid>;
