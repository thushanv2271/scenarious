using Domain.FacilityCashFlowTypes;

namespace Application.FacilityCashFlowTypes.SaveCashFlowType;

/// <summary>
/// Provides user-friendly names for cash flow types
/// Centralizes string constants to avoid duplication
/// </summary>
public static class CashFlowTypeNames
{
    private const string ContractualName = "Contractual Cash Flows";
    private const string ModificationName = "Contract Modification";
    private const string CollateralName = "Collateral Realization";
    private const string LastQuarterName = "Last Quarter Cash Flows";
    private const string OtherName = "Other Cash Flows";
    private const string UnknownName = "Unknown";

    /// <summary>
    /// Gets user-friendly name for cash flow type
    /// </summary>
    public static string GetName(CashFlowsType cashFlowType)
    {
        return cashFlowType switch
        {
            CashFlowsType.ContractualCashFlows => ContractualName,
            CashFlowsType.ContractModification => ModificationName,
            CashFlowsType.CollateralRealization => CollateralName,
            CashFlowsType.LastQuarterCashFlows => LastQuarterName,
            CashFlowsType.OtherCashFlows => OtherName,
            _ => UnknownName
        };
    }
}
