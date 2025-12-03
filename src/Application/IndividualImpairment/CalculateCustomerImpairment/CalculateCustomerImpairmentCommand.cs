using Application.Abstractions.Messaging;

namespace Application.IndividualImpairment.CalculateCustomerImpairment;

/// <summary>
/// Command to calculate individual impairment for all facilities of a customer
/// Aggregates facility-level calculations to customer level
/// </summary>
public sealed record CalculateCustomerImpairmentCommand(
    string CustomerNumber,
    List<FacilityScenarioInput> Facilities,
    bool SaveToDatabase = true
) : ICommand<CustomerImpairmentResponse>;
