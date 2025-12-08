using Application.Abstractions.Messaging;
using Application.FacilityCashFlowTypes.SaveCashFlowType;
using Domain.FacilityCashFlowTypes;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.FacilityCashFlowTypes;

/// <summary>
/// API endpoint for saving facility cash flow type configuration
/// </summary>
internal sealed class SaveCashFlowType : IEndpoint
{
    public sealed record SaveCashFlowTypeRequest(
        string FacilityNumber,
        Guid SegmentId,
        Guid ScenarioId,
        CashFlowsType CashFlowType,
        CashFlowConfigurationDto Configuration
    );

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("cash-flow-types", async (
            SaveCashFlowTypeRequest request,
            ICommandHandler<SaveFacilityCashFlowTypeCommand, SaveFacilityCashFlowTypeResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new SaveFacilityCashFlowTypeCommand(
                request.FacilityNumber,
                request.SegmentId,
                request.ScenarioId,
                request.CashFlowType,
                request.Configuration);

            Result<SaveFacilityCashFlowTypeResponse> result = await handler.Handle(
                command,
                cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.EclAnalysisCashFlowManagement) // More specific permission
        .WithTags("Cash Flow Types")
        .WithName("SaveFacilityCashFlowType")
        .Produces<SaveFacilityCashFlowTypeResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict);
    }
}
