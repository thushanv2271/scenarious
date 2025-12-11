using Application.Abstractions.Messaging;

namespace Application.PD.GetPDProgress;

/// <summary>
/// Query to get or initialize PD progress tracking records
/// </summary>
/// <param name="IsRerun">
/// Optional parameter:
/// - null or false: Return existing active records if they exist, otherwise create new ones
/// - true: Deactivate existing records and create a new set
/// </param>
public sealed record GetPDProgressQuery(bool? IsRerun = null) 
    : IQuery<GetPDProgressResponse>;
