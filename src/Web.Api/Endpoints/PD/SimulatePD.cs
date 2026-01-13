using Application.Abstractions.Messaging;
using Application.PD.SimulatePD;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.PD;

/// <summary>
/// Endpoint for simulating PD calculation progress (testing purposes)
/// </summary>
internal sealed class SimulatePD : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("pd/algorithm/simulate", async (
            SimulatePDRequest request,
            ICommandHandler<SimulatePDCommand> handler,
            CancellationToken cancellationToken) =>
        {
            SimulatePDCommand command = new(request.SessionId, request.DelayMilliseconds);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Accepted(value: new { 
                    command.SessionId,
                    Message = "PD simulation started. Connect to SignalR hub to receive real-time updates."
                }),
                CustomResults.Problem
            );
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("PD Progress");
    }
}

/// <summary>
/// Request model for simulating PD progress
/// </summary>
public sealed record SimulatePDRequest(
    Guid SessionId,
    int DelayMilliseconds = 2000
);
