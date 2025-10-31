using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedKernel;

namespace Domain.EfaConfigs;
public static class EfaConfigurationErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "EfaConfiguration.NotFound",
        $"The EFA configuration with ID '{id}' was not found");

    public static Error YearAlreadyExists(int year) => Error.Conflict(
        "EfaConfiguration.YearAlreadyExists",
        $"An EFA configuration for year {year} already exists");

    public static Error InvalidYear(int year) => Error.Problem(
        "EfaConfiguration.InvalidYear",
        $"The year {year} is invalid");

    public static Error InvalidRate(decimal rate) => Error.Problem(
        "EfaConfiguration.InvalidRate",
        $"The EFA rate {rate} must be greater than or equal to 0");
}
