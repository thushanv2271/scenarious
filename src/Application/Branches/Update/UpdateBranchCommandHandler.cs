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

namespace Application.Branches.Update;

/// <summary>
/// Handles updating existing branches
/// Updates branch details and validates email uniqueness
/// </summary>
internal sealed class UpdateBranchCommandHandler(IApplicationDbContext context)
    : ICommandHandler<UpdateBranchCommand>
{
    public async Task<Result> Handle(UpdateBranchCommand command, CancellationToken cancellationToken)
    {
        //Find the branch to update
        Branch? branch = await context.Branches
            .FirstOrDefaultAsync(b => b.Id == command.BranchId, cancellationToken);

        if (branch is null)
        {
            return Result.Failure(BranchErrors.NotFound(command.BranchId));
        }

        // Check if email is unique (excluding current branch)
        bool emailExists = await context.Branches
            .AnyAsync(b => b.Email == command.Email && b.Id != command.BranchId, cancellationToken);

        if (emailExists)
        {
            return Result.Failure(BranchErrors.EmailNotUnique);
        }

        //Update branch properties
        branch.BranchName = command.BranchName;
        branch.Email = command.Email;
        branch.ContactNumber = command.ContactNumber;
        branch.Address = command.Address;
        branch.IsActive = command.IsActive;
        branch.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
