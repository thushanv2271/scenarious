using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Organizations.DeleteOrganization;

internal sealed class DeleteOrganizationCommandHandler(IApplicationDbContext context)
    : ICommandHandler<DeleteOrganizationCommand>
{
    public async Task<Result> Handle(DeleteOrganizationCommand request, CancellationToken cancellationToken)
    {
        Organization? organization = await context.Organizations
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (organization is null)
        {
            return Result.Failure(OrganizationErrors.NotFound(request.Id));
        }

        // Check if organization has any branches
        bool hasBranches = await context.Branches
            .AnyAsync(b => b.OrganizationId == request.Id, cancellationToken);

        if (hasBranches)
        {
            return Result.Failure(OrganizationErrors.HasBranches);
        }

        // Check if organization has any users (through branches)
        bool hasUsers = await context.Users
            .Include(u => u.Branch)
            .AnyAsync(u => u.Branch != null && u.Branch.OrganizationId == request.Id, cancellationToken);

        if (hasUsers)
        {
            return Result.Failure(OrganizationErrors.HasUsers);
        }

        context.Organizations.Remove(organization);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}