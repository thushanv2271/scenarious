using Application.Abstractions.Messaging;

namespace Application.Organizations.GetOrganization;

public sealed record GetOrganizationQuery(Guid Id) : IQuery<OrganizationResponse>;
