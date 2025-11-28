using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.CollectiveImpairment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;
using System.Text.Json.Nodes;

namespace Application.CollectiveImpairment.GetConfigurations;

internal sealed class GetCollectiveImpairmentConfigsQueryHandler(
    ILogger<GetCollectiveImpairmentConfigsQueryHandler> logger,
    IApplicationDbContext dbContext)
    : IQueryHandler<GetCollectiveImpairmentConfigsQuery, IReadOnlyList<CollectiveImpairmentConfigResponse>>
{
    public async Task<Result<IReadOnlyList<CollectiveImpairmentConfigResponse>>> Handle(
        GetCollectiveImpairmentConfigsQuery query,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching collective impairment config records...");

        IQueryable<CollectiveImpairmentConfig> queryable = dbContext.CollectiveImpairmentConfigs.AsNoTracking();

        if (query.Parameter.HasValue)
        {
            queryable = queryable.Where(c => c.Parameter == query.Parameter.Value);
        }

        List<CollectiveImpairmentConfig> entities = await queryable.ToListAsync(cancellationToken);

        var response = entities.Select(e => new CollectiveImpairmentConfigResponse
        {
            Id = e.Id,
            Parameter = e.Parameter,
            ConfigJson = JsonNode.Parse(e.ConfigJson)?.AsObject() ?? new JsonObject(),
            CreatedDate = e.CreatedDate,
            CreatedBy = e.CreatedBy
        }).ToList();

        logger.LogInformation("{Count} collective impairment config record(s) fetched.", response.Count);

        return Result.Success<IReadOnlyList<CollectiveImpairmentConfigResponse>>(response);
    }
}
