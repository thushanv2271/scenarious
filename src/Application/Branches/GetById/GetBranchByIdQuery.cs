using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Abstractions.Messaging;
using Application.Branches.GetAll;

namespace Application.Branches.GetById;

/// <summary>
/// Query to get a specific branch by ID
/// </summary>
public sealed record GetBranchByIdQuery(Guid BranchId) : IQuery<BranchResponse>;
