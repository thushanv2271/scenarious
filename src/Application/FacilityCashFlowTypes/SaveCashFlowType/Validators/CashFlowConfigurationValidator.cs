using Domain.FacilityCashFlowTypes;
using SharedKernel;

namespace Application.FacilityCashFlowTypes.SaveCashFlowType.Validators;

/// <summary>
/// Validates cash flow configurations based on type
/// Extracted from handler to follow Single Responsibility Principle
/// </summary>
public sealed class CashFlowConfigurationValidator : ICashFlowConfigurationValidator
{
    public Result Validate(CashFlowsType cashFlowType, CashFlowConfigurationDto configuration)
    {
        return cashFlowType switch
        {
            CashFlowsType.ContractualCashFlows => Result.Success(),
            CashFlowsType.ContractModification => ValidateContractModification(configuration),
            CashFlowsType.CollateralRealization => ValidateCollateral(configuration),
            CashFlowsType.LastQuarterCashFlows => ValidateLastQuarter(configuration),
            CashFlowsType.OtherCashFlows => ValidateOtherCashFlows(configuration),
            _ => Result.Failure(FacilityCashFlowTypeErrors.InvalidConfiguration(
                $"Unknown cash flow type: {cashFlowType}"))
        };
    }

    /// <summary>
    /// Validates contract modification configuration
    /// </summary>
    private static Result ValidateContractModification(CashFlowConfigurationDto configuration)
    {
        bool hasParameters = configuration.Frequency.HasValue &&
                            configuration.Value.HasValue &&
                            configuration.TenureMonths.HasValue;

        bool hasFile = configuration.UploadedFileId.HasValue;

        if (!hasParameters && !hasFile)
        {
            return Result.Failure(FacilityCashFlowTypeErrors.InvalidConfiguration(
                "Contract modification requires either payment parameters (frequency, value, tenure) " +
                "or an uploaded payment schedule file"));
        }

        if (hasParameters)
        {
            if (configuration.Value!.Value <= 0)
            {
                return Result.Failure(FacilityCashFlowTypeErrors.InvalidConfiguration(
                    "Value must be greater than zero"));
            }

            if (configuration.TenureMonths!.Value <= 0)
            {
                return Result.Failure(FacilityCashFlowTypeErrors.InvalidConfiguration(
                    "Tenure months must be greater than zero"));
            }

            // FIX: Use generic Enum.IsDefined<TEnum>
            if (!Enum.IsDefined(configuration.Frequency!.Value))
            {
                return Result.Failure(FacilityCashFlowTypeErrors.InvalidConfiguration(
                    "Frequency must be Monthly, Quarterly, Semi-Annually, or Annually"));
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// Validates collateral configuration
    /// </summary>
    private static Result ValidateCollateral(CashFlowConfigurationDto configuration)
    {
        if (!configuration.CollateralValue.HasValue || configuration.CollateralValue.Value <= 0)
        {
            return Result.Failure(FacilityCashFlowTypeErrors.InvalidConfiguration(
                "Collateral value must be greater than zero"));
        }

        if (!configuration.RealizationMonth.HasValue || configuration.RealizationMonth.Value <= 0)
        {
            return Result.Failure(FacilityCashFlowTypeErrors.InvalidConfiguration(
                "Realization month must be greater than zero"));
        }

        if (!configuration.HaircutPercentage.HasValue)
        {
            return Result.Failure(FacilityCashFlowTypeErrors.InvalidConfiguration(
                "Haircut percentage is required"));
        }

        if (configuration.HaircutPercentage.Value < 0 || configuration.HaircutPercentage.Value > 1)
        {
            return Result.Failure(FacilityCashFlowTypeErrors.InvalidConfiguration(
                "Haircut percentage must be between 0 and 1 (e.g., 0.40 for 40%)"));
        }

        return Result.Success();
    }

    /// <summary>
    /// Validates last quarter configuration
    /// </summary>
    private static Result ValidateLastQuarter(CashFlowConfigurationDto configuration)
    {
        if (!configuration.UploadedFileId.HasValue)
        {
            return Result.Failure(FacilityCashFlowTypeErrors.InvalidConfiguration(
                "Uploaded file ID is required for last quarter cash flows"));
        }

        return Result.Success();
    }

    /// <summary>
    /// Validates other cash flows configuration
    /// </summary>
    private static Result ValidateOtherCashFlows(CashFlowConfigurationDto configuration)
    {
        if (configuration.CustomCashFlows == null || !configuration.CustomCashFlows.Any())
        {
            return Result.Failure(FacilityCashFlowTypeErrors.InvalidConfiguration(
                "Custom cash flows are required for other cash flow type"));
        }

        foreach (CustomCashFlowDto cashFlow in configuration.CustomCashFlows)
        {
            if (cashFlow.Month <= 0)
            {
                return Result.Failure(FacilityCashFlowTypeErrors.InvalidConfiguration(
                    "Month number must be greater than zero"));
            }

            if (cashFlow.Amount <= 0)
            {
                return Result.Failure(FacilityCashFlowTypeErrors.InvalidConfiguration(
                    "Cash flow amount must be greater than zero"));
            }
        }

        return Result.Success();
    }
}
