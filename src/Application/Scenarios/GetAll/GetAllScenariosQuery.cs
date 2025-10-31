using Application.Abstractions.Messaging;

namespace Application.Scenarios.GetAll;

/// <summary>
/// Query to get all scenarios grouped by product category and segment
/// </summary>
public sealed record GetAllScenariosQuery(
    Guid? ProductCategoryId = null,
    Guid? SegmentId = null
) : IQuery<List<ProductCategoryWithScenariosResponse>>;
