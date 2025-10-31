using Application.Abstractions.Messaging;
using Application.Files.GetUploadedFile;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Files;

internal sealed class GetUploadedFile : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/files/{id:guid}", async (
            Guid id,
            IQueryHandler<GetUploadedFileQuery, GetUploadedFileResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetUploadedFileQuery(id);
            Result<GetUploadedFileResponse> result = await handler.Handle(query, cancellationToken);

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
