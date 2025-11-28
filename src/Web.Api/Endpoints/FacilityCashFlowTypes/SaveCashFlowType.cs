using Application.Abstractions.Messaging;
using Application.FacilityCashFlowTypes.SaveCashFlowType;
using Domain.FacilityCashFlowTypes;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.FacilityCashFlowTypes;

/// <summary>
/// API endpoint for saving facility cash flow type configurations
/// Supports bulk scenario saving
/// </summary>
internal sealed class SaveCashFlowType : IEndpoint
{
    public sealed record SaveCashFlowTypesRequest(
        string FacilityNumber,
        Guid SegmentId,
        List<ScenarioCashFlowItem> Scenarios
    );

    public sealed record ScenarioCashFlowItem(
        Guid ScenarioId,
        CashFlowsType CashFlowType,
        CashFlowConfigurationDto Configuration
    );

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("cash-flow-types", async (
            SaveCashFlowTypesRequest request,
            ICommandHandler<SaveFacilityCashFlowTypeCommand, SaveFacilityCashFlowTypeResponse> handler,
            CancellationToken cancellationToken) =>
        {
            Result<List<SaveFacilityCashFlowTypeResponse>> result = await SaveBulkCashFlowTypes(
                request,
                handler,
                cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.EclAnalysisCashFlowManagement)
        .WithTags("Cash Flow Types")
        .WithName("SaveFacilityCashFlowTypes")
        .WithDescription("Save cash flow types for one or multiple scenarios")
        .Produces<List<SaveFacilityCashFlowTypeResponse>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<Result<List<SaveFacilityCashFlowTypeResponse>>> SaveBulkCashFlowTypes(
        SaveCashFlowTypesRequest request,
        ICommandHandler<SaveFacilityCashFlowTypeCommand, SaveFacilityCashFlowTypeResponse> handler,
        CancellationToken cancellationToken)
    {
        List<SaveFacilityCashFlowTypeResponse> responses = new();

        foreach (ScenarioCashFlowItem scenarioItem in request.Scenarios)
        {
            SaveFacilityCashFlowTypeCommand command = new(
                request.FacilityNumber,
                request.SegmentId,
                scenarioItem.ScenarioId,
                scenarioItem.CashFlowType,
                scenarioItem.Configuration);

            Result<SaveFacilityCashFlowTypeResponse> result =
                await handler.Handle(command, cancellationToken);

            if (result.IsFailure)
            {
                return Result.Failure<List<SaveFacilityCashFlowTypeResponse>>(result.Error);
            }

            responses.Add(result.Value);
        }

        return Result.Success(responses);
    }
}
