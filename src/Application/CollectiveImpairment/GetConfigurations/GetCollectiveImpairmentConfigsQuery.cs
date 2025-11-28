using Application.Abstractions.Messaging;
using Domain.CollectiveImpairment;

namespace Application.CollectiveImpairment.GetConfigurations;

public sealed record GetCollectiveImpairmentConfigsQuery(ParameterType? Parameter = null)
    : IQuery<IReadOnlyList<CollectiveImpairmentConfigResponse>>;
