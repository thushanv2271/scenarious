using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Organizations.UpdateOrganization;

/// <summary>
/// Handler for updating an existing organization
/// </summary>
internal sealed class UpdateOrganizationCommandHandler(IApplicationDbContext context)
    : ICommandHandler<UpdateOrganizationCommand>
{
    // Handle the command to update an organization
    public async Task<Result> Handle(UpdateOrganizationCommand request, CancellationToken cancellationToken)
    {
        Organization? organization = await context.Organizations
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (organization is null)
        {
            return Result.Failure(OrganizationErrors.NotFound(request.Id));
        }

        // Check if email already exists for another organization
        bool emailExists = await context.Organizations
            .AnyAsync(o => o.Email == request.Email && o.Id != request.Id, cancellationToken);

        if (emailExists)
        {
            return Result.Failure(OrganizationErrors.EmailAlreadyExists(request.Email));
        }

        // Update organization properties
        organization.Name = request.Name;
        organization.Email = request.Email;
        organization.ContactNumber = request.ContactNumber;
        organization.Address = request.Address;
        organization.IsActive = request.IsActive;
        organization.FinancialYearEnd = request.FinancialYearEnd;
        organization.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
