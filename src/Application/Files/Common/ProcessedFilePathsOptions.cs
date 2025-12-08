using System.ComponentModel.DataAnnotations;

namespace Application.Files.Common;

/// <summary>
/// Configuration options for processed file paths based on collective impairment types.
/// </summary>
public sealed class ProcessedFilePathsOptions
{
    public const string SectionName = "ProcessedFilePaths";
    
    /// <summary>
    /// Path for successfully processed PD (Probability of Default) files.
    /// </summary>
    [Required]
    public string PD { get; set; } = string.Empty;
    
    /// <summary>
    /// Path for successfully processed LGD (Loss Given Default) files.
    /// </summary>
    [Required]
    public string LGD { get; set; } = string.Empty;
}