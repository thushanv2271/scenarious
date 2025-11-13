namespace Web.Api.Endpoints.PdCalculation.ExecuteStep3HistoricalPd;

/// <summary>
/// Request for executing PD Calculation Step 3 - Historical PD Generation
/// This step accepts a JSON file containing migration matrices data
/// </summary>
public sealed class Request
{
    /// <summary>
    /// The uploaded JSON file containing migration matrices data
    /// </summary>
    public IFormFile File { get; set; } = default!;
}
