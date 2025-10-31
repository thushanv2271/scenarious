using Application.Abstractions.Messaging;

namespace Application.Scenarios.Create;

/// <summary>
/// Command to create one or more scenarios for a specific product category and segment..
/// </summary>
public sealed record CreateScenarioCommand(
    Guid ProductCategoryId,
    Guid SegmentId,
    List<ScenarioItem> Scenarios
) : ICommand<CreateScenarioResponse>;
