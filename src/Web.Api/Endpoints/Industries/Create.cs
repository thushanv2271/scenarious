using Application.Abstractions.Messaging;
using Application.Industries.Create;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Industries;

internal sealed class Create : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("industries", async (
            CreateIndustryRequest request,
            ICommandHandler<CreateIndustryCommand, CreateIndustryResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateIndustryCommand(
                request.Names
            );

            Result<CreateIndustryResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("Industries");
    }
}