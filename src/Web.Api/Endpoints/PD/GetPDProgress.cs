using Application.Abstractions.Messaging;
using Application.PD.GetPDProgress;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.PD;

/// <summary>
/// Endpoint for retrieving PD progress tracking records
/// </summary>
internal sealed class GetPDProgress : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("pd/progress", async (
            [FromQuery] bool? isRerun,
            IQueryHandler<GetPDProgressQuery, GetPDProgressResponse> handler,
            CancellationToken cancellationToken) =>
        {
            GetPDProgressQuery query = new(isRerun);

            Result<GetPDProgressResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                data => Results.Ok(data),
                CustomResults.Problem
            );
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("PD Progress");
    }
}
