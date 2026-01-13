using Application.Abstractions.Messaging;
using Application.Files.GetPdValidationResults;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Files;

/// <summary>
/// Endpoint for retrieving PD file validation results from JSON configuration
/// </summary>
internal sealed class GetPdValidationResults : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/pd-files/validate/result", async (
            IQueryHandler<GetPdValidationResultsQuery, PaginatedValidationResultsResponse> handler,
            CancellationToken cancellationToken,
            int pageNumber = 1,
            int pageSize = 10,
            string? timePeriod = null,
            string? status = null) =>
        {
            var query = new GetPdValidationResultsQuery(
                PageNumber: pageNumber,
                PageSize: pageSize,
                TimePeriod: timePeriod,
                Status: status
            );

            Result<PaginatedValidationResultsResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                data => Results.Ok(new
                {
                    data = data.Items,
                    total = data.TotalCount,
                    pageNumber = data.PageNumber,
                    pageSize = data.PageSize,
                    totalPages = data.TotalPages
                }),
                CustomResults.Problem
            );
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("PD Files")
        .WithName("GetPdValidationResults")
        .WithSummary("Get PD file validation results from JSON")
        .WithDescription("Retrieves paginated list of PD file validation results from collective_impairment_configs JSON. Status filter options: all, valid, invalid, pending. Can filter by time period.");
    }
}
