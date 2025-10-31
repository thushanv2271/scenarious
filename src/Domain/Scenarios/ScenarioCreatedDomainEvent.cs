using SharedKernel;

namespace Domain.Scenarios;

public sealed record ScenarioCreatedDomainEvent(Guid ScenarioId) : IDomainEvent;
