using Application.Abstractions.Messaging;
using Domain.CollectiveImpairment;
using System.Text.Json.Nodes;

namespace Application.CollectiveImpairment.StoreConfiguration;

public sealed record StoreCollectiveImpairmentConfigCommand(
    ParameterType Parameter,
    JsonObject ConfigJson,
    Guid UserId) : ICommand;
