using Application.Abstractions.Messaging;
using Application.LGD.SimulateLGD;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.LGD;

/// <summary>
/// Endpoint for simulating LGD calculation progress (testing purposes)
/// </summary>
internal sealed class SimulateLgd : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("lgd/algorithm/simulate", async (
            SimulateLgdRequest request,
            ICommandHandler<SimulateLgdCommand> handler,
            CancellationToken cancellationToken) =>
        {
            SimulateLgdCommand command = new(request.SessionId, request.DelayMilliseconds);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Accepted(value: new
                {
                    command.SessionId,
                    Message = "LGD simulation started. Connect to SignalR hub to receive real-time updates."
                }),
                CustomResults.Problem
            );
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess) // Using same permission as PD for now
        .WithTags("LGD Progress");
    }
}