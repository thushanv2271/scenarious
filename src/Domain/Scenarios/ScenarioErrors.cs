using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedKernel;

namespace Domain.Scenarios;
public static class ScenarioErrors
{
    public static Error NotFound(Guid id) => Error.NotFound(
        "Scenario.NotFound",
        $"The scenario with ID '{id}' was not found");

    public static Error InvalidProbabilitySum => Error.Problem(
        "Scenario.InvalidProbabilitySum",
        "The sum of scenario probabilities must equal 100");

    public static Error DuplicateScenarioName => Error.Conflict(
        "Scenario.DuplicateScenarioName",
        "Scenario names must be unique within a segment");
}
