using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedKernel;

namespace Domain.Organizations;

/// <summary>
/// Static class containing all possible error messages for organization operations
/// Used to maintain consistency in error handling across the application
/// </summary>
public static class OrganizationErrors
{
    /// <summary>
    /// Error when organization is not found by ID
    /// </summary>
    /// <param name="id">The organization identifier</param>
    /// <returns>Not found error</returns>
    public static Error NotFound(Guid id) => Error.NotFound(
        "Organization.NotFound",
        $"The organization with ID '{id}' was not found");

    /// <summary>
    /// Error when organization code already exists in the system
    /// </summary>
    /// <param name="code">The organization code</param>
    /// <returns>Conflict error</returns>
    public static Error CodeAlreadyExists(string code) => Error.Conflict(
        "Organization.CodeAlreadyExists",
        $"An organization with code '{code}' already exists");

    /// <summary>
    /// Error when organization email already exists in the system
    /// </summary>
    /// <param name="email">The organization email</param>
    /// <returns>Conflict error</returns>
    public static Error EmailAlreadyExists(string email) => Error.Conflict(
        "Organization.EmailAlreadyExists",
        $"An organization with email '{email}' already exists");

    /// <summary>
    /// Error when organization has associated branches and cannot be deleted
    /// </summary>
    public static Error HasBranches => Error.Conflict(
        "Organization.HasBranches",
        "Cannot delete organization that has branches. Please remove or reassign all branches first.");

    /// <summary>
    /// Error when organization has associated users and cannot be deleted
    /// </summary>
    public static Error HasUsers => Error.Conflict(
        "Organization.HasUsers",
        "Cannot delete organization that has users. Please remove or reassign all users first.");

    /// <summary>
    /// Error when organization has associated data and cannot be deleted
    /// </summary>
    /// <param name="organizationId">The organization identifier</param>
    /// <returns>Conflict error</returns>
    public static Error HasAssociatedData(Guid organizationId) => Error.Conflict(
        "Organization.HasAssociatedData",
        $"Organization with ID '{organizationId}' cannot be deleted because it has associated data");
}