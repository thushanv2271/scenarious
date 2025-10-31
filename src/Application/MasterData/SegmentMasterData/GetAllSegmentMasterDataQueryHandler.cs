using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.MasterData.SegmentMasterData;

internal sealed class GetAllSegmentMasterDataQueryHandler(
    IApplicationDbContext context
) : IQueryHandler<GetAllSegmentMasterDataQuery, List<SegmentListResponse>>
{
    public async Task<Result<List<SegmentListResponse>>> Handle(
        GetAllSegmentMasterDataQuery request,
        CancellationToken cancellationToken)
    {
        List<SegmentListResponse> segments = await context.SegmentMasters
            .AsNoTracking()
            .OrderBy(s => s.Segment)
            .Select(s => new SegmentListResponse
            {
                Id = s.Id,
                Title = s.Segment,
                SubSegments = s.SubSegments,
                CreatedAt = s.CreatedAt,
                ModifiedAt = s.ModifiedAt
            })
            .ToListAsync(cancellationToken);

        return Result.Success(segments);
    }
}
