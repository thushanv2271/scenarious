using Application.Abstractions.Messaging;

namespace Application.Collaterals.GetAll;

/// <summary>
/// Query to get all collaterals.
/// </summary>
public sealed record GetAllCollateralsQuery : IQuery<List<CollateralResponse>>;