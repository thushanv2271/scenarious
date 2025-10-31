using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.EfaConfigs.GetAll;

/// <summary>
/// Handles the query to get all EFA configurations from the database.
/// Returns a list of <see cref="GetAllEfaConfigurationResponse"/> ordered by year (most recent first).
/// </summary>
internal sealed class GetAllEfaConfigurationsQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<GetAllEfaConfigurationsQuery, List<GetAllEfaConfigurationResponse>>
{
    public async Task<Result<List<GetAllEfaConfigurationResponse>>> Handle(
        GetAllEfaConfigurationsQuery query,
        CancellationToken cancellationToken)
    {
        List<GetAllEfaConfigurationResponse> configurations = await context.EfaConfigurations
            .AsNoTracking()
            .OrderByDescending(e => e.Year)
            .Select(e => new GetAllEfaConfigurationResponse(
                e.Id,
                e.Year,
                e.EfaRate,
                e.UpdatedAt,
                e.UpdatedBy
            ))
            .ToListAsync(cancellationToken);

        return Result.Success(configurations);
    }
}
