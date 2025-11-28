namespace Domain.Stages;

/// <summary>
/// Represents a stage mapping option with value and label
/// </summary>
/// <param name="Value">The stage enum value</param>
/// <param name="Label">The display label for the stage</param>
public readonly record struct StageMappingOption(
    Stage Value,
    string Label)
{
    /// <summary>
    /// Gets all available stage mapping options
    /// </summary>
    public static readonly IReadOnlyList<StageMappingOption> All = [
        new(Stage.One, "Stage 1"),
        new(Stage.Two, "Stage 2"), 
        new(Stage.Three, "Stage 3")
    ];

    /// <summary>
    /// Gets a stage mapping option by stage value
    /// </summary>
    /// <param name="stage">The stage to find</param>
    /// <returns>The matching stage mapping option</returns>
    public static StageMappingOption GetByStage(Stage stage) =>
        All.First(option => option.Value == stage);

    /// <summary>
    /// Tries to get a stage mapping option by string value
    /// </summary>
    /// <param name="value">The string representation of the stage</param>
    /// <param name="option">The found stage mapping option</param>
    /// <returns>True if found, false otherwise</returns>
    public static bool TryGetByValue(string value, out StageMappingOption option)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            option = default;
            return false;
        }

        Stage stage = value switch
        {
            "stage1" or "Stage1" or "STAGE1" => Stage.One,
            "stage2" or "Stage2" or "STAGE2" => Stage.Two,
            "stage3" or "Stage3" or "STAGE3" => Stage.Three,
            _ => (Stage)(-1) // Invalid value
        };

        if (stage != (Stage)(-1))
        {
            option = GetByStage(stage);
            return true;
        }

        option = default;
        return false;
    }
}