using Application.Abstractions.Messaging;
using Application.IndividualImpairment.CalculateFacilityImpairment;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.IndividualImpairment;

/// <summary>
/// Endpoint for calculating individual impairment for a single facility.
/// Uses simplified payload - automatically fetches data from database.
/// </summary>
internal sealed class CalculateFacilityImpairment : IEndpoint
{
    /// <summary>
    /// Request model with simplified payload
    /// </summary>
    public sealed record CalculateFacilityImpairmentRequest
    {
        /// <summary>
        /// Facility number to process
        /// </summary>
        public string FacilityNumber { get; init; } = string.Empty;

        /// <summary>
        /// Optional overrides for calculation values
        /// </summary>
        public OverridesInput? Overrides { get; init; }

        /// <summary>
        /// Whether to save the calculation results to database
        /// </summary>
        public bool SaveToDatabase { get; init; } = true;
    }

    /// <summary>
    /// Optional overrides for facility values
    /// </summary>
    public sealed record OverridesInput
    {
        /// <summary>
        /// Override haircut percentage (0-1, e.g., 0.40 for 40%)
        /// </summary>
        public decimal? HaircutPercentage { get; init; }

        /// <summary>
        /// Use only this specific scenario
        /// </summary>
        public Guid? ScenarioId { get; init; }

        /// <summary>
        /// Override amortized cost
        /// </summary>
        public decimal? AmortizedCost { get; init; }

        /// <summary>
        /// Override interest rate (0-1, e.g., 0.10 for 10%)
        /// </summary>
        public decimal? InterestRate { get; init; }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("individual-impairment/calculate-facility", async (
            CalculateFacilityImpairmentRequest request,
            ICommandHandler<CalculateFacilityImpairmentCommand, FacilityImpairmentResponse> handler,
            CancellationToken cancellationToken) =>
        {
            // Map request to command
            var command = new CalculateFacilityImpairmentCommand(
                request.FacilityNumber,
                request.Overrides != null
                    ? new FacilityCalculationOverrides
                    {
                        HaircutPercentage = request.Overrides.HaircutPercentage,
                        ScenarioId = request.Overrides.ScenarioId,
                        AmortizedCost = request.Overrides.AmortizedCost,
                        InterestRate = request.Overrides.InterestRate
                    }
                    : null,
                request.SaveToDatabase
            );

            Result<FacilityImpairmentResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.EclAnalysisAccess)
        .WithTags(Tags.IndividualImpairment)
        .WithName("CalculateFacilityImpairment")
        .WithDescription("Calculate individual impairment for a single facility. " +
                        "Automatically fetches facility data, scenarios, and cash flows from database.")
        .Produces<FacilityImpairmentResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
