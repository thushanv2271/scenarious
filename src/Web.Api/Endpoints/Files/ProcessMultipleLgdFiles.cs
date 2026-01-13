using Application.Abstractions.Messaging;
using Application.Files.ProcessMultipleLgdFiles;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Files;

/// <summary>
/// Endpoint for processing multiple LGD files with validation and analysis.
/// </summary>
internal sealed class ProcessMultipleLgdFiles : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/lgd-files/process-multiple", async (
            [FromBody] ProcessMultipleLgdFilesRequest request,
            ICommandHandler<ProcessMultipleLgdFilesCommand, ProcessMultipleLgdFilesResponse> handler,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Year))
            {
                return CustomResults.Problem(Result.Failure<ProcessMultipleLgdFilesResponse>(
                    new Error("Year.Required", "Year is required", ErrorType.Validation)));
            }

            if (string.IsNullOrWhiteSpace(request.FacilityStatus))
            {
                return CustomResults.Problem(Result.Failure<ProcessMultipleLgdFilesResponse>(
                    new Error("FacilityStatus.Required", "Facility status is required", ErrorType.Validation)));
            }

            var command = new ProcessMultipleLgdFilesCommand(
                Year: request.Year,
                FacilityStatus: request.FacilityStatus
            );

            Result<ProcessMultipleLgdFilesResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("LGD Files")
        .WithName("ProcessMultipleLgdFiles")
        .WithSummary("Process multiple LGD files with validation and analysis")
        .WithDescription("Validates and analyzes LGD files for a specific year and facility status");
    }
}

public sealed record ProcessMultipleLgdFilesRequest(
    string Year,
    string FacilityStatus
);
