using Application.Abstractions.Messaging;

namespace Application.PDAlgorithmResults.UpdateSelectedMethodology;

/// <summary>
/// Command to update the selected methodology for a specific product category and segment
/// </summary>
public sealed record UpdateSelectedMethodologyCommand(
    Guid Id,
    string ProductCategory,
    string Segment,
    string SelectedMethodology
) : ICommand<UpdateSelectedMethodologyResponse>;
