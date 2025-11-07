using Application.Abstractions.Messaging;

namespace Application.Industries.GetAll;

/// <summary>
/// Query to get all industries.
/// </summary>
public sealed record GetAllIndustriesQuery : IQuery<List<IndustryResponse>>;