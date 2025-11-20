using Application.Abstractions.Messaging;

namespace Application.CashFlowProjections.GetFacilityCashFlowOptions;

/// <summary>
/// Query to get available scenarios and cash flow options for a facility
/// </summary>
public sealed record GetFacilityCashFlowOptionsQuery(string FacilityNumber)
    : IQuery<FacilityCashFlowOptionsResponse>;
