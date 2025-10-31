using System;
using System.Text.Json.Nodes;

namespace Application.PD.GetPdSetupData;

/// <summary>
/// DTO for returning PD setup data.
/// </summary>
public sealed class PDSetupDataResponse
{
    public Guid Id { get; init; }
    public JsonObject PDSetupJson { get; init; } = new();
    public DateTime CreatedDate { get; init; }
    public Guid CreatedBy { get; init; }
}
