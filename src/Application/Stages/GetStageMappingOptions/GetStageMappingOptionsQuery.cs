using Application.Abstractions.Messaging;

namespace Application.Stages.GetStageMappingOptions;

/// <summary>
/// Query to get all available stage mapping options
/// </summary>
public sealed record GetStageMappingOptionsQuery : IQuery<GetStageMappingOptionsResponse>;