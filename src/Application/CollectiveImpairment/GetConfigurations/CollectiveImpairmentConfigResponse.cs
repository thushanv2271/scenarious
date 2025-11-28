using Domain.CollectiveImpairment;
using System.Text.Json.Nodes;

namespace Application.CollectiveImpairment.GetConfigurations;

public sealed class CollectiveImpairmentConfigResponse
{
    public Guid Id { get; init; }
    public ParameterType Parameter { get; init; }
    public JsonObject ConfigJson { get; init; } = new();
    public DateTime CreatedDate { get; init; }
    public Guid CreatedBy { get; init; }
}
