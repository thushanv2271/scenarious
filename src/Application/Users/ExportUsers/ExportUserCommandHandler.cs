using Application.Abstractions.Data;
using Application.Abstractions.Exporting;
using Application.Abstractions.Messaging;
using Application.Abstractions.Storage;
using Application.Users.GetAll;
using Domain.Exports;
using SharedKernel;

namespace Application.Users.ExportUsers;

internal sealed class ExportUserCommandHandler(
    IApplicationDbContext context,
    IQueryHandler<GetAllUsersQuery, PaginatedResult<UserListResponse>> getAllUsersHandler,
    IExportService<UserListResponse> exportService,
    IStorageService storageService
) : IQueryHandler<ExportUserCommand, byte[]>
{
    public async Task<Result<byte[]>> Handle(ExportUserCommand command, CancellationToken cancellationToken)
    {
        // 1. Query users
        var getAllQuery = new GetAllUsersQuery(command.PageNumber, command.PageSize, command.Search, command.Filters);
        Result<PaginatedResult<UserListResponse>> result = await getAllUsersHandler.Handle(getAllQuery, cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure<byte[]>(result.Error);
        }

        List<UserListResponse> users = result.Value.Items;

        // 2. Prepare column mappings
        var columnMappings = new Dictionary<string, Func<UserListResponse, object>>
        {
            { "Id", u => u.Id.ToString() },
            { "Email", u => u.Email },
            { "First Name", u => u.FirstName },
            { "Last Name", u => u.LastName },
            { "Status", u => u.UserStatus.ToString() },
            { "Created At", u => u.CreatedAt.ToString(System.Globalization.CultureInfo.InvariantCulture) },
            {
                "Roles", u =>
                {
                    var roleNames = context.UserRoles
                        .Where(ur => ur.UserId == u.Id)
                        .Join(context.Roles,
                            ur => ur.RoleId,
                            r => r.Id,
                            (ur, r) => r.Name)
                        .ToList();

                    return string.Join(", ", roleNames);
                }
            }
        };

        // 3. Generate Excel
        byte[] fileBytes = await exportService.ExportAsync(users, columnMappings, cancellationToken);

        // 4. Save file (local or cloud, depending on IStorageService implementation)
        string fileName = $"UsersExport_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
        string fileLocation = await storageService.SaveAsync(fileBytes, fileName, cancellationToken);

        // 5. Audit details
        var exportAudit = new ExportAudit(
            exportedBy: command.RequestedBy,
            file: fileLocation,
            category: "User Details"
        );

        await context.ExportAudits.AddAsync(exportAudit, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(fileBytes);
    }
}
