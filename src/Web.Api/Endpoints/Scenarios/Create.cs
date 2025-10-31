using Application.Abstractions.Messaging;
using Application.Scenarios.Create;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Scenarios;

internal sealed class Create : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("scenarios", async (
            CreateScenarioRequest request,
            ICommandHandler<CreateScenarioCommand, CreateScenarioResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateScenarioCommand(
                request.ProductCategoryId,
                request.SegmentId,
                [.. request.Scenarios.Select(s => new ScenarioItem(
                    s.ScenarioName,
                    s.Probability,
                    s.ContractualCashFlowsEnabled,
                    s.LastQuarterCashFlowsEnabled,
                    s.OtherCashFlowsEnabled,
                    s.CollateralValueEnabled,
                    s.UploadFile != null ? new UploadFileItem(
                        s.UploadFile.OriginalFileName,
                        s.UploadFile.StoredFileName,
                        s.UploadFile.ContentType,
                        s.UploadFile.Size,
                        s.UploadFile.Url,  // No need for new Uri() anymore
                        s.UploadFile.UploadedBy
                    ) : null
                ))]
            );

            Result<CreateScenarioResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("Scenarios");
    }
}
