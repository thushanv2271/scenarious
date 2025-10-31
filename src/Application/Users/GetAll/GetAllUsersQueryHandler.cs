using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using Domain.Users;
using System.Linq.Expressions;
using SharedKernel.Extensions;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace Application.Users.GetAll;

internal sealed class GetAllUsersQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetAllUsersQuery, PaginatedResult<UserListResponse>>
{
    public async Task<Result<PaginatedResult<UserListResponse>>> Handle(
        GetAllUsersQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<User> query = context.Users.AsQueryable();

        // Apply filters first if provided
        if (request.Filters != null)
        {
            query = query.Where(BuildUserFilterPredicate(request.Filters, context));
        }

        // Apply search after filtering
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(BuildUserSearchPredicate(request.Search));
        }

        int totalCount = await query.CountAsync(cancellationToken);

        List<UserListResponse> users = await query
            .OrderBy(u => u.FirstName)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserListResponse
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                UserStatus = u.UserStatus.ToString(),
                RoleIds = context.UserRoles
                    .Where(ur => ur.UserId == u.Id)
                    .Select(ur => ur.RoleId)
                    .ToList(),

                //obtaining branch details
                BranchId = u.BranchId,
                BranchName = u.BranchId != null
            ? context.Branches
                .Where(b => b.Id == u.BranchId)
                .Select(b => b.BranchName)
                .FirstOrDefault()
            : null,
                BranchCode = u.BranchId != null
            ? context.Branches
                .Where(b => b.Id == u.BranchId)
                .Select(b => b.BranchCode)
                .FirstOrDefault()
            : null,
                CreatedAt = u.CreatedAt,
                ModifiedAt = u.ModifiedAt
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResult<UserListResponse>(users, totalCount);
    }

    private static Expression<Func<User, bool>> BuildUserSearchPredicate(string searchTerm)
    {
        searchTerm = searchTerm.Trim();
        string[] parts = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
        {
            string term = parts[0];
            return u =>
                EF.Functions.ILike(u.FirstName, term + "%") ||
                EF.Functions.ILike(u.LastName, term + "%") ||
                EF.Functions.ILike(u.Email, term + "%");
        }
        else
        {
            string first = parts[0];
            string last = string.Join(' ', parts.Skip(1));
            return u =>
             EF.Functions.ILike(u.FirstName, first + "%") &&
             EF.Functions.ILike(u.LastName, last + "%") ||
             EF.Functions.ILike(u.Email, searchTerm + "%");

        }
    }

    private static Expression<Func<User, bool>> BuildUserFilterPredicate(UserFilters filters, IApplicationDbContext context)
    {
        Expression<Func<User, bool>> predicate = u => true;

        // Status filter
        if (filters.Status.HasValue)
        {
            predicate = predicate.And(u => u.UserStatus == filters.Status.Value);
        }

        // Role filter
        if (filters.RoleIds is { Length: > 0 })
        {
            predicate = predicate.And(u =>
                context.UserRoles.Any(ur => ur.UserId == u.Id && filters.RoleIds.Contains(ur.RoleId)));
        }

        // BRANCH FILTER
        if (filters.BranchIds is { Length: > 0 })
        {
            predicate = predicate.And(u =>
                u.BranchId.HasValue && filters.BranchIds.Contains(u.BranchId.Value));
        }

        // Created Date range filter
        if (filters.CreatedDateRange is not null)
        {
            var start = DateTime.SpecifyKind(filters.CreatedDateRange.Start.Date, DateTimeKind.Utc);
            DateTime end = DateTime.SpecifyKind(filters.CreatedDateRange.End.Date, DateTimeKind.Utc).AddDays(1);

            predicate = predicate.And(u =>
                u.CreatedAt >= start && u.CreatedAt < end);
        }

        // Modified Date range filter
        if (filters.ModifiedDateRange is not null)
        {
            var start = DateTime.SpecifyKind(filters.ModifiedDateRange.Start.Date, DateTimeKind.Utc);
            DateTime end = DateTime.SpecifyKind(filters.ModifiedDateRange.End.Date, DateTimeKind.Utc).AddDays(1);

            predicate = predicate.And(u =>
                u.ModifiedAt >= start && u.ModifiedAt < end);
        }

        return predicate;
    }



}

