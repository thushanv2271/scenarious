using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Abstractions.Messaging;
using Application.Users.ExportUsers;
using Application.Users.GetAll;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class EportUser : IEndpoint
{
    public sealed record ExportUsersRequest(int PageNumber, int PageSize, string? Search, UserFilters? Filters)
    {
        public static ExportUsersRequest Default => new(1, 10, null, null);
    }
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/export", async (
                [AsParameters] ExportUsersRequest request,
                IQueryHandler<ExportUserCommand, byte[]> handler,
                 HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                string? userIdString = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
                {
                    // Use Result.Failure so CustomResults.Problem can accept it
                    var failureResult = Result.Failure(new Error(
                        "InvalidToken",
                        "Invalid token: UserId not found",
                        ErrorType.Validation
                    ));
                    return CustomResults.Problem(failureResult);
                }

                var query = new ExportUserCommand(
                    request.PageNumber,
                    request.PageSize,
                    request.Search,
                    request.Filters,
                    RequestedBy: userId
                );

                Result<byte[]> result = await handler.Handle(query, cancellationToken);

                return result.Match(
                    fileBytes => Results.File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "users.xlsx"),
                    CustomResults.Problem
                );
            })
            .RequireAuthorization()
            .HasPermission(PermissionRegistry.AdminUserManagementRead)
            .WithTags(Tags.Users);

    }
}
