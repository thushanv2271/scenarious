using Application.CashFlowProjections.GetContractualCashFlows;

namespace Application.Abstractions.Calculations;

/// <summary>
/// Service for cash flow projection calculations
/// </summary>
public interface ICashFlowCalculationService
{
    /// <summary>
    /// Generates monthly cash flow projections based on loan parameters
    /// </summary>
    List<MonthlyCashFlow> GenerateCashFlowProjections(
        decimal totalOutstanding,
        decimal annualInterestRate,
        int tenureMonths,
        string installmentType,
        DateTime startDate);

    /// <summary>
    /// Calculates remaining tenure in months from maturity date
    /// </summary>
    int CalculateTenureMonths(DateTime maturityDate);
}
