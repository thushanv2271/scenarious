using Application.Abstractions.Messaging;

namespace Application.CashFlowProjections.GetCollateralCashFlowData;

/// <summary>
/// Query to retrieve collateral value and last quarter cash flow data for a facility
/// </summary>
public sealed record GetCollateralCashFlowDataQuery(string FacilityNumber)
    : IQuery<CollateralCashFlowDataResponse>;
