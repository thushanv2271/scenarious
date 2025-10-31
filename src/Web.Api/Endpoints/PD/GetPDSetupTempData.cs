using Application.Abstractions.Messaging;
using Application.PD.GetPdSetupData;
using SharedKernel;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.PD;

/// <summary>
/// Endpoint for retrieving PD setup temp data for the authenticated user.
/// </summary>
internal sealed class GetPDSetupTempData : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("pd/setup/temp-store", async (
            HttpContext httpContext,
            IQueryHandler<GetPdSetupDataQuery, IReadOnlyList<PDSetupDataResponse>> handler,
            ILogger<GetPDSetupTempData> logger,
            CancellationToken cancellationToken) =>
        {
            GetPdSetupDataQuery query = new();

            Result<IReadOnlyList<PDSetupDataResponse>> result;
            try
            {
                result = await handler.Handle(query, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while retrieving PD setup temp data.");
                return Results.Problem("An unexpected error occurred.");
            }

            return result.Match(
                data => Results.Ok(data),
                CustomResults.Problem
            );
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("PD Setup");
    }
}