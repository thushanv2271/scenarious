using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Collaterals.GetAll;

internal sealed class GetAllCollateralsQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<GetAllCollateralsQuery, List<CollateralResponse>>
{
    public async Task<Result<List<CollateralResponse>>> Handle(
        GetAllCollateralsQuery request,
        CancellationToken cancellationToken)
    {
        List<CollateralResponse> collaterals = await context.Collaterals
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(collateral => new CollateralResponse(
                collateral.Id,
                collateral.Name,
                collateral.CreatedAt,
                collateral.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        return Result.Success(collaterals);
    }
}