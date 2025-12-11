using SharedKernel;

namespace Domain.IndividualImpairment;

public static class IndividualImpairmentErrors
{
    public static Error InvalidAmortizedCost => Error.Validation(
        "IndividualImpairment.InvalidAmortizedCost",
        "Amortized cost must be greater than zero");

    public static Error InvalidInterestRate => Error.Validation(
        "IndividualImpairment.InvalidInterestRate",
        "Interest rate must be between 0 and 1");

    public static Error NoScenarios => Error.Validation(
        "IndividualImpairment.NoScenarios",
        "At least one scenario is required");

    public static Error InvalidProbabilitySum => Error.Validation(
        "IndividualImpairment.InvalidProbabilitySum",
        "Scenario probabilities must sum to 1.00 (100%)");

    public static Error InvalidScenarioProbability => Error.Validation(
        "IndividualImpairment.InvalidScenarioProbability",
        "Scenario probability must be between 0 and 1");

    public static Error NoCashFlows => Error.Validation(
        "IndividualImpairment.NoCashFlows",
        "Each scenario must have at least one cash flow");

    public static Error InvalidCashFlowMonth => Error.Validation(
        "IndividualImpairment.InvalidCashFlowMonth",
        "Cash flow month must be greater than zero");

    public static Error InvalidCashFlowAmount => Error.Validation(
        "IndividualImpairment.InvalidCashFlowAmount",
        "Cash flow amount must be greater than zero");

    public static Error FacilityNotFound(string facilityNumber) => Error.NotFound(
        "IndividualImpairment.FacilityNotFound",
        $"Facility with number '{facilityNumber}' was not found");

    public static Error NoFacilitiesProvided => Error.Validation(
        "IndividualImpairment.NoFacilitiesProvided",
        "At least one facility must be provided");
}
