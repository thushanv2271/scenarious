using Application.Abstractions.Messaging;

namespace Application.IndividualImpairment.CalculateCustomerImpairment;

/// <summary>
/// Command to calculate individual impairment for all facilities of a customer
/// Uses simplified payload - fetches data from database automatically
/// </summary>
public sealed record CalculateCustomerImpairmentCommand(
    string CustomerNumber,
    List<FacilityOverrideInput>? Facilities = null,
    bool SaveToDatabase = true
) : ICommand<CustomerImpairmentResponse>;

/// <summary>
/// Optional facility input with override capability
/// If not provided, all facilities for the customer will be processed
/// </summary>
public sealed record FacilityOverrideInput
{
    /// <summary>
    /// Facility number to process
    /// </summary>
    public string FacilityNumber { get; init; } = string.Empty;

    /// <summary>
    /// Optional overrides for specific values
    /// </summary>
    public FacilityOverrides? Overrides { get; init; }
}

/// <summary>
/// Optional overrides for facility-level values
/// These override the values fetched from the database
/// </summary>
public sealed record FacilityOverrides
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
    /// Override interest rate
    /// </summary>
    public decimal? InterestRate { get; init; }
}
