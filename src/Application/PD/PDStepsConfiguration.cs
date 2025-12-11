namespace Application.PD;

/// <summary>
/// Represents a PD calculation step with its subtasks
/// </summary>
public record PDStepDefinition(
    string StepName,
    int StepOrder,
    List<PDSubTaskDefinition> SubTasks
);

/// <summary>
/// Represents a subtask within a PD calculation step
/// </summary>
public record PDSubTaskDefinition(
    string SubTaskName,
    int SubTaskOrder
);

/// <summary>
/// Configuration for PD calculation steps and subtasks
/// </summary>
public static class PDStepsConfiguration
{
    public static List<PDStepDefinition> GetDefaultSteps() => new()
    {
        new PDStepDefinition(
            StepName: "Fetch Configuration from Database",
            StepOrder: 1,
            SubTasks: new List<PDSubTaskDefinition>
            {
                new("Extract TimeConfig", 1),
                new("Fetch Date Passed Due Buckets", 2),
                new("Extract other configurations", 3)
            }
        ),
        new PDStepDefinition(
            StepName: "Data Preparation",
            StepOrder: 2,
            SubTasks: new List<PDSubTaskDefinition>
            {
                new("Load and validate input data", 1),
                new("Save data into database", 2),
                new("Prepare data for matrix generation", 3)
            }
        ),
        new PDStepDefinition(
            StepName: "Matrix Generation",
            StepOrder: 3,
            SubTasks: new List<PDSubTaskDefinition>
            {
                new("Generate transition matrix", 1)
            }
        ),
        new PDStepDefinition(
            StepName: "Historical PD Calculation",
            StepOrder: 4,
            SubTasks: new List<PDSubTaskDefinition>
            {
                new("Calculate historical default rates", 1)
            }
        ),
        new PDStepDefinition(
            StepName: "Extrapolation",
            StepOrder: 5,
            SubTasks: new List<PDSubTaskDefinition>
            {
                new("Perform PD extrapolation", 1)
            }
        )
    };
}
