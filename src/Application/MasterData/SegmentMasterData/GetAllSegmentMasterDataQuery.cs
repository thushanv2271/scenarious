using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.MasterData.SegmentMasterData;

public sealed record GetAllSegmentMasterDataQuery()
    : IQuery<List<SegmentListResponse>>;
