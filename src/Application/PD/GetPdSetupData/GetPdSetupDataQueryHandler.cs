using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.PDTempData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;
using SharedKernel;

namespace Application.PD.GetPdSetupData;

/// <summary>
/// Handles the GetPdSetupDataQuery.
/// </summary>
internal sealed class GetPdSetupDataQueryHandler(
    ILogger<GetPdSetupDataQueryHandler> logger,
    IApplicationDbContext dbContext)
    : IQueryHandler<GetPdSetupDataQuery, IReadOnlyList<PDSetupDataResponse>>
{
    public async Task<Result<IReadOnlyList<PDSetupDataResponse>>> Handle(
        GetPdSetupDataQuery query,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching PD setup data records...");

        List<PDTempData> entities = await dbContext.PDTempDatas
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var response = entities.Select(e => new PDSetupDataResponse
        {
            Id = e.Id,
            PDSetupJson = JsonNode.Parse(e.PDSetupJson)?.AsObject() ?? new JsonObject(),
            CreatedDate = e.CreatedDate,
            CreatedBy = e.CreatedBy
        }).ToList();

        logger.LogInformation("{Count} PD setup data record(s) fetched.", response.Count);

        return Result.Success<IReadOnlyList<PDSetupDataResponse>>(response);
    }
}
