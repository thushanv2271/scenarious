using Application.Abstractions.Messaging;
using Application.IndividualImpairment.CalculateFacilityImpairment;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.IndividualImpairment;

/// <summary>
/// Endpoint for calculating individual impairment for a facility.
/// Processes multiple scenarios with cash flows and calculates present value and impairment amount.
/// </summary>
internal sealed class CalculateFacilityImpairment : IEndpoint
{
    /// <summary>
    /// Maps the HTTP POST endpoint for calculating facility impairment.
    /// </summary>
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("individual-impairment/calculate-facility", async (
            CalculateFacilityImpairmentCommand command,
            ICommandHandler<CalculateFacilityImpairmentCommand, FacilityImpairmentResponse> handler,
            CancellationToken cancellationToken) =>
        {
            // Execute the command
            Result<FacilityImpairmentResponse> result = await handler.Handle(command, cancellationToken);

            // Return success or failure response
            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.IndividualImpairment)
        .WithName("CalculateFacilityImpairment")
        .Produces<FacilityImpairmentResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
