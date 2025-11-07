namespace Web.Api.Endpoints.Industries;

/// <summary>
/// Request to create multiple industries.
/// </summary>
public sealed record CreateIndustryRequest(
    string[] Names
);