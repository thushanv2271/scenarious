using Application.Abstractions.Messaging;

namespace Application.LGD.GetLGDProgress;

/// <summary>
/// Query to get LGD progress tracking status
/// </summary>
public sealed record GetLgdProgressQuery(bool? IsRerun) : IQuery<GetLgdProgressResponse>;
