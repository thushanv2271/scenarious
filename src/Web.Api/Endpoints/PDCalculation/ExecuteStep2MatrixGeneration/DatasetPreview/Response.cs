namespace Web.Api.Endpoints.PdCalculation.ExecuteStep2MatrixGeneration.DatasetPreview;

/// <summary>
/// Response model for Step 2 dataset preview endpoint
/// </summary>
public sealed class Response
{
    /// <summary>
    /// Gets or sets the collection of PD migration datasets
    /// </summary>
    public IReadOnlyList<Application.DTOs.PD.PdMigrationDataset> Datasets { get; set; } = [];

    /// <summary>
    /// Gets or sets the total number of datasets
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the data was retrieved
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
