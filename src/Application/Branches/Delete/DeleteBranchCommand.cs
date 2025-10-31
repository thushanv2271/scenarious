using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Abstractions.Messaging;

namespace Application.Branches.Delete;

/// <summary>
/// Command to delete a branch
/// Will fail if branch has assigned users
/// </summary>
public sealed record DeleteBranchCommand(Guid BranchId) : ICommand;
