using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Branches.GetAll;

/// <summary>
/// Response object containing branch information
/// Used when sending branch data to the frontend
/// </summary>
public sealed record BranchResponse
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public string BranchCode { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string ContactNumber { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
