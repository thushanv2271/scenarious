using Application.Abstractions.Messaging;

namespace Application.PD.GetPdSetupData;

/// <summary>
/// Query to get all PD setup data.
/// </summary>
public sealed record GetPdSetupDataQuery : IQuery<IReadOnlyList<PDSetupDataResponse>>;
