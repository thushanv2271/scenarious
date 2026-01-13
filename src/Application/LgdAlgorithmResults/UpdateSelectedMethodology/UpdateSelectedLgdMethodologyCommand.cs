using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.LgdAlgorithmResults.UpdateSelectedMethodology;

/// <summary>
/// Command to update the selected methodology for a specific product category and segment in LGD Algorithm Results
/// </summary>
public sealed record UpdateSelectedLgdMethodologyCommand(
    Guid Id,
    string ProductCategory,
    string Segment,
    string SelectedMethodology)
    : ICommand<UpdateSelectedLgdMethodologyResponse>;