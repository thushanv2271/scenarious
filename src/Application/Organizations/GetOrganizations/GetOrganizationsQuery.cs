using Application.Abstractions.Messaging;

namespace Application.Organizations.GetOrganizations;

public sealed record GetOrganizationsQuery(
    bool? IsActive = null,  
    string? SearchTerm = null
) : IQuery<List<OrganizationResponse>>;
