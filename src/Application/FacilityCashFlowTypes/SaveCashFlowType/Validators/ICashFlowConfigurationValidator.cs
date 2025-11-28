using Domain.FacilityCashFlowTypes;
using SharedKernel;

namespace Application.FacilityCashFlowTypes.SaveCashFlowType.Validators;

/// <summary>
/// Interface for validating cash flow configurations
/// </summary>
public interface ICashFlowConfigurationValidator
{
    /// <summary>
    /// Validates configuration based on cash flow type
    /// </summary>
    Result Validate(CashFlowsType cashFlowType, CashFlowConfigurationDto configuration);
}
