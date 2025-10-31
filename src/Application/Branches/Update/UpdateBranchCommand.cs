using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Abstractions.Messaging;

namespace Application.Branches.Update;

/// <summary>
/// Command to update an existing branch
/// Note: Branch code and organization cannot be changed
/// </summary>
public sealed record UpdateBranchCommand(
    Guid BranchId,
    string BranchName,
    string Email,
    string ContactNumber,
    string Address,
    bool IsActive
) : ICommand; // Returns: Success or failure
