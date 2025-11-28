using System.Collections.Generic;
using Application.Abstractions.Messaging;

namespace Application.RiskEvaluations.GetCustomerEvaluations;

public sealed record GetCustomerEvaluationsQuery(
    string CustomerNumber
) : IQuery<List<CustomerEvaluationResponse>>;
