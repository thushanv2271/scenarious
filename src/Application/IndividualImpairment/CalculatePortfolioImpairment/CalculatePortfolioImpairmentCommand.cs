using Application.Abstractions.Messaging;

namespace Application.IndividualImpairment.CalculatePortfolioImpairment;

/// <summary>
/// Command to calculate impairment for multiple customers (portfolio level)
/// </summary>
public sealed record CalculatePortfolioImpairmentCommand(
    List<string> CustomerNumbers,
    string? BranchCode = null,
    bool SaveToDatabase = true
) : ICommand<PortfolioImpairmentResponse>;
