using Application.Abstractions.Messaging;
using Application.EfaConfigs.GetAll;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.EfaConfigs;

/// <summary>
/// Endpoint for retrieving all EFA configurations.
/// </summary>
internal sealed class GetAll : IEndpoint
{
    /// <summary>
    /// Maps the HTTP GET endpoint for fetching all EFA configurations.
    /// </summary>
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("efa-configurations", async (
            IQueryHandler<GetAllEfaConfigurationsQuery, List<GetAllEfaConfigurationResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            // Create query
            var query = new GetAllEfaConfigurationsQuery();

            // Execute query
            Result<List<GetAllEfaConfigurationResponse>> result = await handler.Handle(query, cancellationToken);

            // Return success or failure response
            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.AdminSettingsRolePermissionRead)
        .WithTags("EFA Configurations");
    }
}
