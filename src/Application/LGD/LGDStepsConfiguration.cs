namespace Application.LGD;

/// <summary>
/// Represents an LGD calculation step with its subtasks
/// </summary>
public record LgdStepDefinition(
    string StepName,
    int StepOrder,
    List<LgdSubTaskDefinition> SubTasks
);

/// <summary>
/// Represents a subtask within an LGD calculation step
/// </summary>
public record LgdSubTaskDefinition(
    string SubTaskName,
    int SubTaskOrder
);

/// <summary>
/// Configuration for LGD calculation steps and subtasks
/// </summary>
public static class LgdStepsConfiguration
{
    public static List<LgdStepDefinition> GetDefaultSteps() => new()
    {
        new LgdStepDefinition(
            StepName: "Data Preparation",
            StepOrder: 1,
            SubTasks: new List<LgdSubTaskDefinition>
            {
                new("Load and validate input data", 1),
                new("Process LGD file extraction", 2)
            }
        ),
        new LgdStepDefinition(
            StepName: "LGD Discounted Cashflow",
            StepOrder: 2,
            SubTasks: new List<LgdSubTaskDefinition>
            {
                new("Generate discounted cashflow summary", 1)
            }
        ),
        new LgdStepDefinition(
            StepName: "LGD Yearly Average",
            StepOrder: 3,
            SubTasks: new List<LgdSubTaskDefinition>
            {
                new("Calculate yearly LGD averages", 1)
            }
        ),
        new LgdStepDefinition(
            StepName: "VC Point Determination",
            StepOrder: 4,
            SubTasks: new List<LgdSubTaskDefinition>
            {
                new("Determine optimal VC conversion points", 1)
            }
        ),
        new LgdStepDefinition(
            StepName: "VC_LGD Discounted Cashflow",
            StepOrder: 5,
            SubTasks: new List<LgdSubTaskDefinition>
            {
                new("Generate VC_LGD discounted cashflow summary", 1)
            }
        ),
        new LgdStepDefinition(
            StepName: "Financial Year Analysis",
            StepOrder: 6,
            SubTasks: new List<LgdSubTaskDefinition>
            {
                new("Analyze LGD and VC_LGD financial year results", 1)
            }
        ),
        new LgdStepDefinition(
            StepName: "Final Result Combination",
            StepOrder: 7,
            SubTasks: new List<LgdSubTaskDefinition>
            {
                new("Combine and finalize LGD calculation results", 1)
            }
        )
    };
}