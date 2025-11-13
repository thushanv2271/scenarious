using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.PD;

/// <summary>
/// Represents the macro economic factor adjustment table (EFA values per year)
/// </summary>
public sealed class MacroEconomicFactorAdjustmentDto
{
    /// <summary>
    /// Gets or sets the year-to-EFA mappings
    /// </summary>
    public List<EfaYearValueDto> EfaValues { get; set; } = new();
}

/// <summary>
/// Represents a single year's EFA value
/// </summary>
public sealed class EfaYearValueDto
{
    /// <summary>
    /// Gets or sets the year (1–5)
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Gets or sets the EFA value (percentage)
    /// </summary>
    public decimal EfaPercentage { get; set; }
}

