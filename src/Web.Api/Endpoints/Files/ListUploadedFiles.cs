using Application.Abstractions.Messaging;
using Application.Files.ListUploadedFiles;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Files;

internal sealed class ListUploadedFiles : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/files", async (
            IQueryHandler<ListUploadedFilesQuery, List<ListUploadedFilesResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            Result<List<ListUploadedFilesResponse>> result = await handler.Handle(new ListUploadedFilesQuery(), cancellationToken);

            return result.Match(
                data => Results.Ok(data),
                CustomResults.Problem
            );
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.PDSetupAccess)
        .WithTags("Files");
    }
}
