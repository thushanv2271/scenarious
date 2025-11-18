using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.FacilityCashFlowTypes.SaveCashFlowType;
/// <summary>
/// Represents a custom cash flow entry
/// </summary>
public sealed record CustomCashFlowDto(
    int Month,
    decimal Amount,
    string? Description = null
);

