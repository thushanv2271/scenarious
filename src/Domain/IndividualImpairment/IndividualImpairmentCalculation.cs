using SharedKernel;

namespace Domain.IndividualImpairment;

public sealed class IndividualImpairmentCalculation
{
    public Guid Id { get; private set; }
    public string FacilityNumber { get; private set; } = string.Empty;
    public string CustomerNumber { get; private set; } = string.Empty;
    public DateTime CalculationDate { get; private set; }
    public decimal InterestRate { get; private set; }
    public decimal AmortizedCost { get; private set; }
    public decimal SumOfPVOfCashFlows { get; private set; }
    public decimal ImpairmentAmount { get; private set; }
    public decimal ImpairmentPercentage { get; private set; }
    public string ScenarioDetailsJson { get; private set; } = string.Empty;
    public Guid CalculatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static IndividualImpairmentCalculation Create(
        string facilityNumber,
        string customerNumber,
        decimal interestRate,
        decimal amortizedCost,
        decimal sumOfPVOfCashFlows,
        decimal impairmentAmount,
        string scenarioDetailsJson,
        Guid calculatedBy)
    {
        var calculation = new IndividualImpairmentCalculation
        {
            Id = Guid.NewGuid(),
            FacilityNumber = facilityNumber,
            CustomerNumber = customerNumber,
            CalculationDate = DateTime.UtcNow,
            InterestRate = interestRate,
            AmortizedCost = amortizedCost,
            SumOfPVOfCashFlows = sumOfPVOfCashFlows,
            ImpairmentAmount = impairmentAmount,
            ImpairmentPercentage = amortizedCost > 0
                ? impairmentAmount / amortizedCost * 100
                : 0,
            ScenarioDetailsJson = scenarioDetailsJson,
            CalculatedBy = calculatedBy,
            CreatedAt = DateTime.UtcNow
        };

        return calculation;
    }
}
