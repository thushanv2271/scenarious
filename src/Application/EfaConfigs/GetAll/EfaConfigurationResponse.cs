using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.EfaConfigs.GetAll;

/// <summary>
/// Represents the data returned when retrieving an EFA configuration.
/// Contains details such as the configuration ID, year, rate, and update information.
/// </summary>
public sealed class EfaConfigurationResponse
{
    public Guid Id { get; init; }
    public int Year { get; init; }
    public decimal EfaRate { get; init; }
    public DateTime UpdatedAt { get; init; }
    public Guid UpdatedBy { get; init; }
}
