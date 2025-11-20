using Application.Abstractions.Messaging;

namespace Application.CashFlowProjections.GetCustomerFacilities;

/// <summary>
/// Query to get all facilities for a specific customer
/// </summary>
public sealed record GetCustomerFacilitiesQuery(string CustomerNumber)
    : IQuery<List<CustomerFacilityResponse>>;
