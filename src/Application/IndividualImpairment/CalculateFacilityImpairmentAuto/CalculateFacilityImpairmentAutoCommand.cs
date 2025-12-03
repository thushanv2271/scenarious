using Application.Abstractions.Messaging;
using Application.IndividualImpairment.CalculateFacilityImpairment;

namespace Application.IndividualImpairment.CalculateFacilityImpairmentAuto;

/// <summary>
/// Command to automatically calculate facility impairment by fetching all required data
/// from saved configurations and portfolio snapshot
/// </summary>
public sealed record CalculateFacilityImpairmentAutoCommand(
    string FacilityNumber
) : ICommand<FacilityImpairmentResponse>;
