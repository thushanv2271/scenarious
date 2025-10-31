using Application.Abstractions.Messaging;
using Application.Users.GetAll;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class GetAll : IEndpoint
{
    public sealed record GetAllUsersRequest(int PageNumber, int PageSize, string? Search, UserFilters? Filters)
    {
        public static GetAllUsersRequest Default => new(1, 10, null, null);
    }
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/search", async (
            [AsParameters] GetAllUsersRequest request,
            IQueryHandler<GetAllUsersQuery, PaginatedResult<UserListResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAllUsersQuery(
                request.PageNumber,
                request.PageSize,
                request.Search,
                request.Filters
            );

            Result<PaginatedResult<UserListResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .HasPermission(PermissionRegistry.AdminUserManagementRead)
        .WithTags(Tags.Users);
    }
}
