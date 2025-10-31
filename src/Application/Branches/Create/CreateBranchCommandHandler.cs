using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Branches;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Branches.Create;

/// <summary>
/// Handler for creating new branches
/// </summary>
internal sealed class CreateBranchCommandHandler(IApplicationDbContext context)
    : ICommandHandler<CreateBranchCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateBranchCommand command, CancellationToken cancellationToken)
    {
        // Verify organization exists
        bool organizationExists = await context.Organizations
            .AnyAsync(o => o.Id == command.OrganizationId, cancellationToken);

        if (!organizationExists)
        {
            return Result.Failure<Guid>(BranchErrors.OrganizationNotFound);
        }

        // Check if branch code is unique
        bool codeExists = await context.Branches
            .AnyAsync(b => b.BranchCode == command.BranchCode, cancellationToken);

        if (codeExists)
        {
            return Result.Failure<Guid>(BranchErrors.CodeNotUnique);
        }

        // Check if email is unique
        bool emailExists = await context.Branches
            .AnyAsync(b => b.Email == command.Email, cancellationToken);

        if (emailExists)
        {
            return Result.Failure<Guid>(BranchErrors.EmailNotUnique);
        }

        //Create new object
        var branch = new Branch
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = command.OrganizationId,
            BranchName = command.BranchName,
            BranchCode = command.BranchCode,
            Email = command.Email,
            ContactNumber = command.ContactNumber,
            Address = command.Address,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        //Save to database
        context.Branches.Add(branch);
        await context.SaveChangesAsync(cancellationToken);

        // Return the new branch ID
        return Result.Success(branch.Id);
    }
}
