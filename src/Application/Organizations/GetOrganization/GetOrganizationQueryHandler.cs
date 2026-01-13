using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Organizations.GetOrganization;

/// <summary>
/// Handler for retrieving a single organization by ID
/// </summary>
internal sealed class GetOrganizationQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetOrganizationQuery, OrganizationResponse> 
{
    // Handle the query to get an organization by ID
    public async Task<Result<OrganizationResponse>> Handle(GetOrganizationQuery request, CancellationToken cancellationToken)
    {
        OrganizationResponse? organization = await context.Organizations
            .Where(o => o.Id == request.Id)
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
            .FirstOrDefaultAsync(cancellationToken);

        if (organization is null)
        {
            return Result.Failure<OrganizationResponse>(OrganizationErrors.NotFound(request.Id));
        }

        return Result.Success(organization);
    }
}
