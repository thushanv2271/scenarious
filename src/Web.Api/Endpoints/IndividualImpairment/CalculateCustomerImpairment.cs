using Application.Abstractions.Messaging;
using Application.IndividualImpairment.CalculateCustomerImpairment;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.IndividualImpairment;

/// <summary>
/// Endpoint for calculating individual impairment for a customer's facilities.
/// Uses simplified payload - automatically fetches data from database.
/// </summary>
internal sealed class CalculateCustomerImpairment : IEndpoint
{
    /// <summary>
    /// Request model with simplified payload
    /// </summary>
    public sealed record CalculateCustomerImpairmentRequest
    {
        /// <summary>
        /// Customer number to process
        /// </summary>
        public string CustomerNumber { get; init; } = string.Empty;

        /// <summary>
        /// Optional list of specific facilities to process
        /// If not provided, all facilities for the customer will be processed
        /// </summary>
        public List<FacilityInput>? Facilities { get; init; }

        /// <summary>
        /// Whether to save the calculation results to database
        /// </summary>
        public bool SaveToDatabase { get; init; } = true;
    }

    /// <summary>
    /// Facility input with optional overrides
    /// </summary>
    public sealed record FacilityInput
    {
        /// <summary>
        /// Facility number
        /// </summary>
        public string FacilityNumber { get; init; } = string.Empty;

        /// <summary>
        /// Optional overrides for this facility
        /// </summary>
        public OverridesInput? Overrides { get; init; }
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
        app.MapPost("individual-impairment/calculate-customer", async (
            CalculateCustomerImpairmentRequest request,
            ICommandHandler<CalculateCustomerImpairmentCommand, CustomerImpairmentResponse> handler,
            CancellationToken cancellationToken) =>
        {
            // Map request to command
            var command = new CalculateCustomerImpairmentCommand(
                request.CustomerNumber,
                request.Facilities?.Select(f => new FacilityOverrideInput
                {
                    FacilityNumber = f.FacilityNumber,
                    Overrides = f.Overrides != null
                        ? new FacilityOverrides
                        {
                            HaircutPercentage = f.Overrides.HaircutPercentage,
                            ScenarioId = f.Overrides.ScenarioId,
                            AmortizedCost = f.Overrides.AmortizedCost,
                            InterestRate = f.Overrides.InterestRate
                        }
                        : null
                }).ToList(),
                request.SaveToDatabase
            );

            Result<CustomerImpairmentResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.EclAnalysisAccess)
        .WithTags(Tags.IndividualImpairment)
        .WithName("CalculateCustomerImpairment")
        .WithDescription("Calculate individual impairment for a customer's facilities. " +
                        "Automatically fetches facility data, scenarios, and cash flows from database.")
        .Produces<CustomerImpairmentResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
