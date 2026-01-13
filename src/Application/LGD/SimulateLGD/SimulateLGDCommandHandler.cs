using Application.Abstractions.Messaging;
using Application.LGD.Services;
using Domain.LGDProgressTrackings;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.LGD.SimulateLGD;

#pragma warning disable CA5394 // Random is acceptable for simulation purposes

/// <summary>
/// Handler for simulating LGD calculation progress
/// </summary>
internal sealed class SimulateLgdCommandHandler(ILgdProgressPublisher publisher, ILogger<SimulateLgdCommandHandler> logger)
    : ICommandHandler<SimulateLgdCommand>
{
    public async Task<Result> Handle(SimulateLgdCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting LGD simulation for SessionId: {SessionId} with delay: {Delay}ms", request.SessionId, request.DelayMilliseconds);

        // Get default steps configuration
        List<LgdStepDefinition> steps = LgdStepsConfiguration.GetDefaultSteps();
        logger.LogInformation("Loaded {StepCount} steps for LGD simulation", steps.Count);

        foreach (LgdStepDefinition step in steps)
        {
            foreach (LgdSubTaskDefinition subTask in step.SubTasks)
            {
                logger.LogInformation("Publishing InProgress for LGD Step {StepOrder}, SubTask {SubTaskOrder}", step.StepOrder, subTask.SubTaskOrder);

                // Publish subtask started
                await publisher.PublishProgress(
                    request.SessionId,
                    step.StepOrder,
                    subTask.SubTaskOrder,
                    LgdProgressStatus.InProgress,
                    cancellationToken: cancellationToken
                );

                // Simulate work
                await Task.Delay(request.DelayMilliseconds, cancellationToken);

                // Randomly fail some tasks (10% chance)
                var random = new Random();
                if (random.Next(0, 100) < 10)
                {
                    logger.LogInformation("Publishing Failed for LGD Step {StepOrder}, SubTask {SubTaskOrder}", step.StepOrder, subTask.SubTaskOrder);

                    await publisher.PublishProgress(
                        request.SessionId,
                        step.StepOrder,
                        subTask.SubTaskOrder,
                        LgdProgressStatus.Failed,
                        "Simulated failure for demonstration",
                        cancellationToken
                    );

                    logger.LogInformation("LGD simulation ended with failure");
                    return Result.Success();
                }

                logger.LogInformation("Publishing Completed for LGD Step {StepOrder}, SubTask {SubTaskOrder}", step.StepOrder, subTask.SubTaskOrder);

                // Publish subtask completed
                await publisher.PublishProgress(
                    request.SessionId,
                    step.StepOrder,
                    subTask.SubTaskOrder,
                    LgdProgressStatus.Completed,
                    cancellationToken: cancellationToken
                );
            }
        }

        logger.LogInformation("LGD simulation completed successfully");
        return Result.Success();
    }
}
