using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Files;
using Domain.ProductCategories;
using Domain.Scenarios;
using Domain.Segments;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Scenarios.Create;

internal sealed class CreateScenarioCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateScenarioCommand, CreateScenarioResponse>
{
    public async Task<Result<CreateScenarioResponse>> Handle(
        CreateScenarioCommand command,
        CancellationToken cancellationToken)
    {
        // Validate probability sum
        decimal totalProbability = command.Scenarios.Sum(s => s.Probability);
        if (Math.Abs(totalProbability - 100) > 0.01m)
        {
            return Result.Failure<CreateScenarioResponse>(
                ScenarioErrors.InvalidProbabilitySum);
        }

        // Verify segment exists
        bool segmentExists = await context.Segments
            .AnyAsync(s => s.Id == command.SegmentId, cancellationToken);

        if (!segmentExists)
        {
            return Result.Failure<CreateScenarioResponse>(
                SegmentErrors.NotFound(command.SegmentId));
        }

        // Check for duplicate scenario names
        var scenarioNames = command.Scenarios.Select(s => s.ScenarioName).ToList();
        int distinctCount = scenarioNames.Distinct().Count();  // Fixed: Get the count, not the method
        bool hasDuplicates = scenarioNames.Count != distinctCount;

        if (hasDuplicates)
        {
            return Result.Failure<CreateScenarioResponse>(
                ScenarioErrors.DuplicateScenarioName);
        }

        // Check if scenario names already exist for this segment
        bool existingNames = await context.Scenarios
            .AnyAsync(s => s.SegmentId == command.SegmentId &&
                          scenarioNames.Contains(s.ScenarioName),
                     cancellationToken);

        if (existingNames)
        {
            return Result.Failure<CreateScenarioResponse>(
                ScenarioErrors.DuplicateScenarioName);
        }

        var scenarioIds = new List<Guid>();

        foreach (ScenarioItem item in command.Scenarios)
        {
            Guid? uploadedFileId = null;

            // Create uploaded file if provided
            if (item.UploadFile is not null)
            {
                var uploadedFile = new UploadedFile
                {
                    Id = Guid.CreateVersion7(),
                    OriginalFileName = item.UploadFile.OriginalFileName,
                    StoredFileName = item.UploadFile.StoredFileName,
                    ContentType = item.UploadFile.ContentType,
                    Size = item.UploadFile.Size,
                    PhysicalPath = string.Empty, // Not used in this context
                    PublicUrl = item.UploadFile.Url.ToString(),  // Convert Uri to string
                    UploadedBy = item.UploadFile.UploadedBy,
                    UploadedAt = DateTimeOffset.UtcNow
                };

                context.UploadedFiles.Add(uploadedFile);
                uploadedFileId = uploadedFile.Id;
            }

            // Create scenario
            var scenario = new Scenario
            {
                Id = Guid.CreateVersion7(),
                SegmentId = command.SegmentId,
                ScenarioName = item.ScenarioName,
                Probability = item.Probability,
                ContractualCashFlowsEnabled = item.ContractualCashFlowsEnabled,
                LastQuarterCashFlowsEnabled = item.LastQuarterCashFlowsEnabled,
                OtherCashFlowsEnabled = item.OtherCashFlowsEnabled,
                CollateralValueEnabled = item.CollateralValueEnabled,
                UploadedFileId = uploadedFileId,
                CreatedAt = dateTimeProvider.UtcNow,
                UpdatedAt = dateTimeProvider.UtcNow
            };

            scenario.Raise(new ScenarioCreatedDomainEvent(scenario.Id));
            context.Scenarios.Add(scenario);
            scenarioIds.Add(scenario.Id);
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateScenarioResponse(
            Success: true,
            Data: new ScenarioData(command.SegmentId, scenarioIds)
        ));
    }
}
