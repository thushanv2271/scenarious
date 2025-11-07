using Application.Abstractions.Messaging;
using Application.RiskEvaluations.GetCustomerEvaluations;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.RiskEvaluations;

internal sealed class GetCustomerEvaluations : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("risk-evaluations/customer/{customerNumber}", async (
            string customerNumber,
            IQueryHandler<GetCustomerEvaluationsQuery, List<CustomerEvaluationResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCustomerEvaluationsQuery(customerNumber);
            Result<List<CustomerEvaluationResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("Risk Evaluations");
    }
}
