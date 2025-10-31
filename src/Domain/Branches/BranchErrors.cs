using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedKernel;

namespace Domain.Branches;

/// <summary>
/// Contains all error messages related to branch operations
/// Used when branch operations fail (not found, duplicate, etc.)
/// </summary>
public static class BranchErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Branch.NotFound",
        $"The branch with ID '{id}' was not found");

    public static Error NotFoundByCode(string code) => Error.NotFound(
        "Branch.NotFoundByCode",
        $"The branch with code '{code}' was not found");

    //validation contexts where branch doesn't exist
    public static Error InvalidBranchId(Guid id) => Error.Problem(
        "Branch.InvalidBranchId",
        $"The branch with ID '{id}' does not exist");

    public static Error CodeNotUnique => Error.Conflict(
        "Branch.CodeNotUnique",
        "The branch code must be unique");

    public static Error EmailNotUnique => Error.Conflict(
        "Branch.EmailNotUnique",
        "The branch email must be unique");

    public static Error OrganizationNotFound => Error.NotFound(
        "Branch.OrganizationNotFound",
        "The specified organization does not exist");

    public static Error HasAssignedUsers => Error.Conflict(
    "Branch.HasAssignedUsers",
    "Cannot delete branch because it has users assigned to it");
}
