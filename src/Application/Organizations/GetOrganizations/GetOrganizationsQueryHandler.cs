using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Branches.GetAll;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Organizations.GetOrganizations;

internal sealed class GetOrganizationsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetOrganizationsQuery, List<OrganizationResponse>>
{   
    public async Task<Result<List<OrganizationResponse>>> Handle(
        GetOrganizationsQuery request, 
        CancellationToken cancellationToken)
    {
        IQueryable<Domain.Organizations.Organization> query = context.Organizations.AsQueryable();

        // Apply filters
        if (request.IsActive.HasValue)
        {
            query = query.Where(o => o.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(o => 
                o.Name.Contains(request.SearchTerm) ||
                o.Code.Contains(request.SearchTerm) ||
                o.Email.Contains(request.SearchTerm));
        }

        List<OrganizationResponse> organizations = await query
            .OrderBy(o => o.Name)
            .Select(o => new OrganizationResponse(
                o.Id,
                o.Name,
                o.Code,
                o.Email,
                o.ContactNumber,
                o.Address,
                o.IsActive,
                o.FinancialYearEnd,
                o.CreatedAt,
                o.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        return Result.Success(organizations);
    }
}
