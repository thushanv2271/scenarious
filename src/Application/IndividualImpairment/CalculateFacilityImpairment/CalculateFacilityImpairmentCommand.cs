using Application.Abstractions.Messaging;

namespace Application.IndividualImpairment.CalculateFacilityImpairment;

/// <summary>
/// Command to calculate individual impairment for a single facility
/// Uses simplified payload - fetches data from database automatically
/// </summary>
public sealed record CalculateFacilityImpairmentCommand(
    string FacilityNumber,
    FacilityCalculationOverrides? Overrides = null,
    bool SaveToDatabase = true
) : ICommand<FacilityImpairmentResponse>;

/// <summary>
/// Optional overrides for facility calculation values
/// These override the values fetched from the database
/// </summary>
public sealed record FacilityCalculationOverrides
{
    /// <summary>
    /// Override haircut percentage (0-1, e.g., 0.40 for 40%)
    /// </summary>
    public decimal? HaircutPercentage { get; init; }

    /// <summary>
    /// Override to use a specific scenario only
    /// If not provided, all configured scenarios are used
    /// </summary>
    public Guid? ScenarioId { get; init; }

    /// <summary>
    /// Override amortized cost (total outstanding)
    /// </summary>
    public decimal? AmortizedCost { get; init; }

    /// <summary>
    /// Override interest rate (0-1, e.g., 0.10 for 10%)
    /// </summary>
    public decimal? InterestRate { get; init; }
}
