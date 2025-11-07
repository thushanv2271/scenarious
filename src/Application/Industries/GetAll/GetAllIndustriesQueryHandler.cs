using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Industries.GetAll;

internal sealed class GetAllIndustriesQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<GetAllIndustriesQuery, List<IndustryResponse>>
{
    public async Task<Result<List<IndustryResponse>>> Handle(
        GetAllIndustriesQuery request,
        CancellationToken cancellationToken)
    {
        List<IndustryResponse> industries = await context.Industries
            .AsNoTracking()
            .OrderBy(i => i.Name)
            .Select(industry => new IndustryResponse(
                industry.Id,
                industry.Name,
                industry.CreatedAt,
                industry.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        return Result.Success(industries);
    }
}