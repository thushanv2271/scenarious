using Application.Abstractions.Messaging;

namespace Application.Organizations.DeleteOrganization;

public sealed record DeleteOrganizationCommand(Guid Id) : ICommand;
