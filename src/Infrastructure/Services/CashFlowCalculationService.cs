using Application.Abstractions.Calculations;
using Application.CashFlowProjections.Common;
using Application.CashFlowProjections.GetContractualCashFlows;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Implementation of cash flow calculation service
/// Handles EMI, bullet payment, and equal principal calculations
/// </summary>
internal sealed class CashFlowCalculationService : ICashFlowCalculationService
{
    private readonly ILogger<CashFlowCalculationService> _logger;

    // Installment type keywords
    private const string EqualInstallmentKeyword = "Equal";
    private const string BulletInstallmentKeyword = "Bullet";

    public CashFlowCalculationService(ILogger<CashFlowCalculationService> logger)
    {
        _logger = logger;
    }

    public List<MonthlyCashFlow> GenerateCashFlowProjections(
        decimal totalOutstanding,
        decimal annualInterestRate,
        int tenureMonths,
        string installmentType,
        DateTime startDate)
    {
        var cashFlows = new List<MonthlyCashFlow>();
        decimal monthlyInterestRate = annualInterestRate / 100 / 12;
        decimal remainingPrincipal = totalOutstanding;

        for (int month = 1; month <= tenureMonths; month++)
        {
            decimal interestAmount = remainingPrincipal * monthlyInterestRate;
            decimal principalAmount = CalculatePrincipalAmount(
                totalOutstanding,
                remainingPrincipal,
                monthlyInterestRate,
                tenureMonths,
                installmentType,
                month);

            decimal totalAmount = principalAmount + interestAmount;
            remainingPrincipal -= principalAmount;

            cashFlows.Add(new MonthlyCashFlow
            {
                Month = month,
                PrincipalAmount = Math.Round(principalAmount, 2),
                InterestAmount = Math.Round(interestAmount, 2),
                TotalAmount = Math.Round(totalAmount, 2),
                PaymentDate = startDate.AddMonths(month)
            });
        }

        return cashFlows;
    }

    public int CalculateTenureMonths(DateTime maturityDate)
    {
        int tenureMonths = (maturityDate.Year - DateTime.UtcNow.Year) * 12 +
                           maturityDate.Month - DateTime.UtcNow.Month;

        if (tenureMonths <= 0)
        {
            _logger.LogWarning("Facility has already matured, using minimum tenure");
            return CashFlowConstants.MinimumTenureMonths;
        }

        return tenureMonths;
    }

    /// <summary>
    /// Calculates principal amount based on installment type
    /// </summary>
    private decimal CalculatePrincipalAmount(
        decimal totalOutstanding,
        decimal remainingPrincipal,
        decimal monthlyInterestRate,
        int tenureMonths,
        string installmentType,
        int currentMonth)
    {
        // EMI - Equal monthly installments
        if (installmentType.Contains(EqualInstallmentKeyword, StringComparison.OrdinalIgnoreCase))
        {
            decimal emi = CalculateEMI(totalOutstanding, monthlyInterestRate, tenureMonths);
            decimal interestAmount = remainingPrincipal * monthlyInterestRate;
            return emi - interestAmount;
        }

        // Bullet payment - all principal in last month
        if (installmentType.Contains(BulletInstallmentKeyword, StringComparison.OrdinalIgnoreCase))
        {
            return currentMonth == tenureMonths ? remainingPrincipal : 0;
        }

        // Equal principal - divide principal equally across months
        return totalOutstanding / tenureMonths;
    }

    /// <summary>
    /// Calculates EMI (Equated Monthly Installment)
    /// </summary>
    private static decimal CalculateEMI(decimal principal, decimal monthlyRate, int months)
    {
        if (monthlyRate == 0)
        {
            return principal / months;
        }

        decimal numerator = principal * monthlyRate * (decimal)Math.Pow((double)(1 + monthlyRate), months);
        decimal denominator = (decimal)Math.Pow((double)(1 + monthlyRate), months) - 1;

        return numerator / denominator;
    }
}
