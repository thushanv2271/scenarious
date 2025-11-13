namespace Domain.FacilityCashFlowTypes;

/// <summary>
/// Enumeration of available cash flow types for facilities
/// </summary>
public enum CashFlowsType
{
    /// <summary>
    /// Contractual cash flows based on original loan terms
    /// </summary>
    ContractualCashFlows = 1,

    /// <summary>
    /// Modified contract terms with new payment schedule
    /// </summary>
    ContractModification = 2,

    /// <summary>
    /// Cash flows from collateral realization
    /// </summary>
    CollateralRealization = 3,

    /// <summary>
    /// Historical cash flows from last quarter
    /// </summary>
    LastQuarterCashFlows = 4,

    /// <summary>
    /// Custom/other cash flow patterns
    /// </summary>
    OtherCashFlows = 5
}
