using Application.Abstractions.Messaging;
using Domain.FacilityCashFlowTypes;

namespace Application.FacilityCashFlowTypes.SaveCashFlowType;

/// <summary>
/// Command to save a cash flow type configuration for a facility
/// </summary>
public sealed record SaveFacilityCashFlowTypeCommand(
    string FacilityNumber,
    Guid SegmentId,
    Guid ScenarioId,
    CashFlowsType CashFlowType,
    CashFlowConfigurationDto Configuration
) : ICommand<SaveFacilityCashFlowTypeResponse>;
