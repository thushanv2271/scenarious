using Application.Abstractions.Messaging;
using Application.PD.Services;
using Domain.PDProgressTrackings;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.PD.SimulatePD;

#pragma warning disable CA5394 // Random is acceptable for simulation purposes

/// <summary>
/// Handler for simulating PD calculation progress
/// </summary>
internal sealed class SimulatePDCommandHandler(IPDProgressPublisher publisher, ILogger<SimulatePDCommandHandler> logger) 
    : ICommandHandler<SimulatePDCommand>
{
    public async Task<Result> Handle(SimulatePDCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting PD simulation for SessionId: {SessionId} with delay: {Delay}ms", request.SessionId, request.DelayMilliseconds);
        
        // Get default steps configuration
        List<PDStepDefinition> steps = PDStepsConfiguration.GetDefaultSteps();
        logger.LogInformation("Loaded {StepCount} steps for simulation", steps.Count);

        foreach (PDStepDefinition step in steps)
        {
            foreach (PDSubTaskDefinition subTask in step.SubTasks)
            {
                logger.LogInformation("Publishing InProgress for Step {StepOrder}, SubTask {SubTaskOrder}", step.StepOrder, subTask.SubTaskOrder);
                
                // Publish subtask started
                await publisher.PublishProgress(
                    request.SessionId,
                    step.StepOrder,
                    subTask.SubTaskOrder,
                    PDProgressStatus.InProgress,
                    cancellationToken: cancellationToken
                );

                // Simulate work being done
                await Task.Delay(request.DelayMilliseconds, cancellationToken);

                // 10% chance of random error for testing
                bool shouldFail = Random.Shared.Next(100) < 10;

                if (shouldFail)
                {
                    logger.LogError("Simulating failure for Step {StepOrder}, SubTask {SubTaskOrder}", step.StepOrder, subTask.SubTaskOrder);
                    
                    // Publish failure
                    await publisher.PublishProgress(
                        request.SessionId,
                        step.StepOrder,
                        subTask.SubTaskOrder,
                        PDProgressStatus.Failed,
                        $"Simulated error in {subTask.SubTaskName}",
                        cancellationToken
                    );

                    // Stop simulation on error and return success (failure communicated via SignalR)
                    logger.LogInformation("Stopping simulation due to failure at Step {StepOrder}, SubTask {SubTaskOrder}", step.StepOrder, subTask.SubTaskOrder);
                    return Result.Success();
                }

                logger.LogInformation("Publishing Completed for Step {StepOrder}, SubTask {SubTaskOrder}", step.StepOrder, subTask.SubTaskOrder);
                
                // Publish subtask completed
                await publisher.PublishProgress(
                    request.SessionId,
                    step.StepOrder,
                    subTask.SubTaskOrder,
                    PDProgressStatus.Completed,
                    cancellationToken: cancellationToken
                );
            }
        }

        logger.LogInformation("PD simulation completed successfully for SessionId: {SessionId}", request.SessionId);
        return Result.Success();
    }
}

