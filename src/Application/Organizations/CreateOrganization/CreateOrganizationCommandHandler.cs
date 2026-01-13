using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Organizations.CreateOrganization;

/// <summary>
/// Handler for creating new organizations  
/// </summary>
internal sealed class CreateOrganizationCommandHandler(IApplicationDbContext context)
    : ICommandHandler<CreateOrganizationCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateOrganizationCommand request, CancellationToken cancellationToken)
    {
        // Check if organization code already exists
        bool codeExists = await context.Organizations
            .AnyAsync(o => o.Code == request.Code, cancellationToken);

        if (codeExists)
        {
                return Result.Failure<Guid>(OrganizationErrors.CodeAlreadyExists(request.Code));
        }

        // Check if organization email already exists
        bool emailExists = await context.Organizations
            .AnyAsync(o => o.Email == request.Email, cancellationToken);

        if (emailExists)
        {
            return Result.Failure<Guid>(OrganizationErrors.EmailAlreadyExists(request.Email));
        }

        var organization = new Organization
        {
            Id = Guid.CreateVersion7(),
            Name = request.Name,
            Code = request.Code,
            Email = request.Email,
            ContactNumber = request.ContactNumber,
            Address = request.Address,
            IsActive = request.IsActive,
            FinancialYearEnd = request.FinancialYearEnd,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Organizations.Add(organization);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(organization.Id);
    }
}
