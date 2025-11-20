using Application.Abstractions.Messaging;

namespace Application.CashFlowProjections.GetContractualCashFlows;

/// <summary>
/// Query to retrieve contractual cash flows for a facility from portfolio snapshot
/// </summary>
public sealed record GetContractualCashFlowsQuery(string FacilityNumber)
    : IQuery<ContractualCashFlowsResponse>;
