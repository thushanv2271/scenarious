using Application.Abstractions.Messaging;
using Application.Files.GetFileValidationResults;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Files;

internal sealed class GetFileValidationResults : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/files/validate/result", async (
            IQueryHandler<GetFileValidationResultsQuery, PaginatedResult<FileValidationResultResponse>> handler,
            CancellationToken cancellationToken,
            int pageNumber = 1,
            int pageSize = 10,
            string? status = null,
            string? collectiveImpairmentType = null,
            string? timePeriod = null) =>
        {
            var query = new GetFileValidationResultsQuery(pageNumber, pageSize, status, collectiveImpairmentType, timePeriod);
            Result<PaginatedResult<FileValidationResultResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                data => Results.Ok(new
                {
                    data = data.Items,
                    total = data.TotalCount,
                    pageNumber,
                    pageSize
                }),
                CustomResults.Problem
            );
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("Files")
        .WithName("GetFileValidationResults")
        .WithSummary("Get file validation results")
        .WithDescription("Retrieves paginated list of file validation results with statistics. Status filter options: all, valid, invalid. Can filter by CollectiveImpairmentType and TimePeriod based on file paths.");
    }
}