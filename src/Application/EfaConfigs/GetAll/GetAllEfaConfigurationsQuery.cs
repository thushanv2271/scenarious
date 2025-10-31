using Application.Abstractions.Messaging;

namespace Application.EfaConfigs.GetAll;

/// <summary>
/// Represents a request to retrieve all EFA configurations.
/// Returns a list of <see cref="GetAllEfaConfigurationResponse"/> objects.
/// </summary>
public sealed record GetAllEfaConfigurationsQuery : IQuery<List<GetAllEfaConfigurationResponse>>;
