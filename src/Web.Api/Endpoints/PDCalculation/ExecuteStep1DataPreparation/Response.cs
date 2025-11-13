namespace Web.Api.Endpoints.PdCalculation.ExecuteStep1DataPreparation;

/// <summary>
/// Response for Step 1 - Data Preparation
/// </summary>
/// <param name="Success">Indicates if the operation was successful</param>
/// <param name="Message">Descriptive message about the operation</param>
/// <param name="Timestamp">Timestamp when the operation completed</param>
public sealed record Response(
    bool Success,
    string Message,
    DateTime Timestamp
);
