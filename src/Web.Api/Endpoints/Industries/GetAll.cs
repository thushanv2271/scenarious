using Application.Abstractions.Messaging;
using Application.Industries.GetAll;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Industries;

internal sealed class GetAll : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("industries", async (
            IQueryHandler<GetAllIndustriesQuery, List<IndustryResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAllIndustriesQuery();

            Result<List<IndustryResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("Industries");
    }
}