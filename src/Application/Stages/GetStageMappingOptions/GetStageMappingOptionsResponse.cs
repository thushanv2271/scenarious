namespace Application.Stages.GetStageMappingOptions;

/// <summary>
/// Response containing stage mapping options
/// </summary>
/// <param name="Options">Collection of stage mapping options</param>
public sealed record GetStageMappingOptionsResponse(
    IEnumerable<StageMappingOptionDto> Options);

/// <summary>
/// Data transfer object for stage mapping option
/// </summary>
/// <param name="Value">The stage value as string</param>
/// <param name="Label">The display label</param>
public sealed record StageMappingOptionDto(
    string Value,
    string Label);