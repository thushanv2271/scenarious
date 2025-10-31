using Application.Abstractions.Messaging;
using Application.MasterData.SegmentMasterData;
using SharedKernel;
using Microsoft.Extensions.Logging;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.MasterData;

internal sealed class GetAllSegmentMasterDataEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("master-data/segments", async (
            IQueryHandler<GetAllSegmentMasterDataQuery, List<SegmentListResponse>> handler,
            ILogger<GetAllSegmentMasterDataEndpoint> logger,
            CancellationToken cancellationToken) =>
        {
            GetAllSegmentMasterDataQuery query = new();

            Result<List<SegmentListResponse>> result;
            try
            {
                result = await handler.Handle(query, cancellationToken);

                return result.Match(
                    data => Results.Ok(data),
                    CustomResults.Problem
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while retrieving Masterdata.");
                return Results.Problem("An unexpected error occurred.");
            }
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("Master Data");
    }
}
