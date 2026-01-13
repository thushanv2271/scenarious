using Application.Abstractions.Messaging;
using Application.LGD.GetLGDProgress;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LGD;

/// <summary>
/// Endpoint for retrieving LGD progress tracking records
/// </summary>
internal sealed class GetLgdProgress : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("lgd/progress", async (
            [FromQuery] bool? isRerun,
            IQueryHandler<GetLgdProgressQuery, GetLgdProgressResponse> handler,
            CancellationToken cancellationToken) =>
        {
            GetLgdProgressQuery query = new(isRerun);

            Result<GetLgdProgressResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                data => Results.Ok(data),
                CustomResults.Problem
            );
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess) // Using same permission as PD for now
        .WithTags("LGD Progress");
    }
}