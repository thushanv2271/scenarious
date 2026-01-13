using Application.Abstractions.Messaging;
using Application.Files.GetLgdFileValidationResults;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Files;

/// <summary>
/// Endpoint for retrieving LGD file validation results with pagination.
/// </summary>
/// <remarks>
/// Returns validation results in the format:
/// {
///   "data": [{ validation result objects }],
///   "total": total count,
///   "pageNumber": current page,
///   "pageSize": items per page
/// }
/// 
/// Status values: "Valid" or "Invalid"
/// </remarks>
internal sealed class GetLgdFileValidationResults : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/lgd-files/validation-results", async (
            [FromQuery] string year,
            [FromQuery] string facilityStatus,
            [FromQuery] string? status,
            [FromQuery] int pageNumber,
            [FromQuery] int pageSize,
            IQueryHandler<GetLgdFileValidationResultsQuery, LgdFileValidationResultsResponse> handler,
            CancellationToken cancellationToken) =>
        {
            // Set defaults if not provided
            if (pageNumber <= 0)
            {
                pageNumber = 1;
            }
            
            if (pageSize <= 0)
            {
                pageSize = 10;
            }

            var query = new GetLgdFileValidationResultsQuery(
                Year: year,
                FacilityStatus: facilityStatus,
                Status: status,
                PageNumber: pageNumber,
                PageSize: pageSize
            );

            Result<LgdFileValidationResultsResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("LGD Files")
        .WithName("GetLgdFileValidationResults")
        .WithSummary("Get LGD file validation results with pagination")
        .WithDescription("Retrieve validation results for LGD files filtered by year and facility status. Returns data with structure: { data: [], total: number, pageNumber: number, pageSize: number }. Status values: Valid or Invalid.");
    }
}
