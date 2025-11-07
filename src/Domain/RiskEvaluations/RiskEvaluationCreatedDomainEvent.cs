using SharedKernel;

namespace Domain.RiskEvaluations;

// Domain event raised when a risk evaluation is created
public sealed record RiskEvaluationCreatedDomainEvent(Guid EvaluationId) : IDomainEvent;
