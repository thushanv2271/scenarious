using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Abstractions.Messaging;

namespace Application.Branches.GetAll;

/// <summary>
/// Query to get all branches
/// Can optionally filter by organization
/// </summary>
public sealed record GetAllBranchesQuery(Guid? OrganizationId = null)
    : IQuery<List<BranchResponse>>; // Returns: List of branches
