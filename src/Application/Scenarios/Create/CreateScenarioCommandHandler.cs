using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Files;
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
    private const decimal ProbabilityTolerance = 0.01m;
    private const decimal RequiredProbabilitySum = 100m;

    public async Task<Result<CreateScenarioResponse>> Handle(
        CreateScenarioCommand command,
        CancellationToken cancellationToken)
    {
        // Validate probability sum
        decimal totalProbability = command.Scenarios.Sum(s => s.Probability);
        if (Math.Abs(totalProbability - RequiredProbabilitySum) > ProbabilityTolerance)
        {
            return Result.Failure<CreateScenarioResponse>(
                ScenarioErrors.InvalidProbabilitySum);
        }

        // Verify segment exists and get segment with product category details
        Segment? segment = await context.Segments
            .Include(s => s.ProductCategory)
            .FirstOrDefaultAsync(s => s.Id == command.SegmentId, cancellationToken);

        if (segment is null)
        {
            return Result.Failure<CreateScenarioResponse>(
                SegmentErrors.NotFound(command.SegmentId));
        }

        // Get product category details
        Domain.ProductCategories.ProductCategory? productCategory = await context.ProductCategories
            .FirstOrDefaultAsync(pc => pc.Id == segment.ProductCategoryId, cancellationToken);

        if (productCategory is null)
        {
            return Result.Failure<CreateScenarioResponse>(
                Error.NotFound("ProductCategory.NotFound", "Product category not found"));
        }

        // Check for duplicate scenario names
        var scenarioNames = command.Scenarios.Select(s => s.ScenarioName).ToList();
        int distinctCount = scenarioNames.Distinct().Count();
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

        var createdScenarios = new List<CreatedScenarioDetailResponse>();

        foreach (ScenarioItem item in command.Scenarios)
        {
            Guid? uploadedFileId = null;
            UploadedFileDetailResponse? uploadedFileDetail = null;

            // Reference existing uploaded file by ID
            if (item.UploadFile is not null)
            {
                // Verify the file exists
                UploadedFile? existingFile = await context.UploadedFiles
                    .FirstOrDefaultAsync(f => f.Id == item.UploadFile.Id, cancellationToken);

                if (existingFile is null)
                {
                    return Result.Failure<CreateScenarioResponse>(
                        Error.NotFound("UploadedFile.NotFound",
                            $"Uploaded file with ID '{item.UploadFile.Id}' was not found"));
                }

                uploadedFileId = existingFile.Id;  // ← Use existing file ID

                uploadedFileDetail = new UploadedFileDetailResponse(
                    existingFile.Id,
                    existingFile.OriginalFileName,
                    existingFile.StoredFileName,
                    existingFile.ContentType,
                    existingFile.Size,
                    new Uri(existingFile.PublicUrl), // Convert string to Uri
                    existingFile.UploadedBy,
                    existingFile.UploadedAt
                );
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

            // Add to response list
            createdScenarios.Add(new CreatedScenarioDetailResponse(
                scenario.Id,
                scenario.ScenarioName,
                scenario.Probability,
                scenario.ContractualCashFlowsEnabled,
                scenario.LastQuarterCashFlowsEnabled,
                scenario.OtherCashFlowsEnabled,
                scenario.CollateralValueEnabled,
                uploadedFileDetail,
                scenario.CreatedAt,
                scenario.UpdatedAt
            ));
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateScenarioResponse(
            Success: true,
            Data: new ScenarioDataResponse(
                segment.Id,
                segment.Name,
                productCategory.Id,
                productCategory.Name,
                createdScenarios
            )
        ));
    }
}
