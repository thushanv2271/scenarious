using System.Text.Json;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.FacilityCashFlowTypes.SaveCashFlowType.Validators;
using Domain.FacilityCashFlowTypes;
using Domain.Scenarios;
using Domain.Segments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.FacilityCashFlowTypes.SaveCashFlowType;

/// <summary>
/// Handler to save facility cash flow type configuration
/// Delegates validation to specialized validators
/// </summary>
internal sealed class SaveFacilityCashFlowTypeCommandHandler(
    IApplicationDbContext context,
    ILoanDetailsRepository loanRepository,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider,
    ICashFlowConfigurationValidator configurationValidator,
    ILogger<SaveFacilityCashFlowTypeCommandHandler> logger)
    : ICommandHandler<SaveFacilityCashFlowTypeCommand, SaveFacilityCashFlowTypeResponse>
{
    public async Task<Result<SaveFacilityCashFlowTypeResponse>> Handle(
        SaveFacilityCashFlowTypeCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Validate segment exists
            Segment? segment = await context.Segments
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == command.SegmentId, cancellationToken);

            if (segment is null)
            {
                logger.LogWarning("Segment not found: {SegmentId}", command.SegmentId);
                return Result.Failure<SaveFacilityCashFlowTypeResponse>(
                    FacilityCashFlowTypeErrors.SegmentNotFound(command.SegmentId));
            }

            // Step 2: Validate scenario exists and is linked to segment
            Scenario? scenario = await context.Scenarios
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == command.ScenarioId, cancellationToken);

            if (scenario is null)
            {
                logger.LogWarning("Scenario not found: {ScenarioId}", command.ScenarioId);
                return Result.Failure<SaveFacilityCashFlowTypeResponse>(
                    FacilityCashFlowTypeErrors.ScenarioNotFound(command.ScenarioId));
            }

            if (scenario.SegmentId != command.SegmentId)
            {
                logger.LogWarning(
                    "Scenario {ScenarioId} not linked to segment {SegmentId}",
                    command.ScenarioId, command.SegmentId);
                return Result.Failure<SaveFacilityCashFlowTypeResponse>(
                    FacilityCashFlowTypeErrors.ScenarioNotLinkedToSegment);
            }

            // Step 3: Validate facility exists and matches segment
            FacilityBasicDetail? facilityDetail = await loanRepository
                .GetFacilityBasicDetailsAsync(command.FacilityNumber, cancellationToken);

            if (facilityDetail == null)
            {
                logger.LogWarning("Facility not found: {FacilityNumber}", command.FacilityNumber);
                return Result.Failure<SaveFacilityCashFlowTypeResponse>(
                    FacilityCashFlowTypeErrors.FacilityNotFound(command.FacilityNumber));
            }

            if (!facilityDetail.Segment.Equals(segment.Name, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning(
                    "Facility segment mismatch. Expected: {ExpectedSegment}, Found: {ActualSegment}",
                    segment.Name, facilityDetail.Segment);
                return Result.Failure<SaveFacilityCashFlowTypeResponse>(
                    FacilityCashFlowTypeErrors.FacilitySegmentMismatch);
            }

            // Step 4: Check for duplicate active configuration
            bool existingActiveType = await context.FacilityCashFlowTypes
                .AnyAsync(f =>
                    f.FacilityNumber == command.FacilityNumber &&
                    f.ScenarioId == command.ScenarioId &&
                    f.IsActive,
                    cancellationToken);

            if (existingActiveType)
            {
                logger.LogWarning(
                    "Active cash flow type already exists for facility {FacilityNumber} and scenario {ScenarioId}",
                    command.FacilityNumber, command.ScenarioId);
                return Result.Failure<SaveFacilityCashFlowTypeResponse>(
                    FacilityCashFlowTypeErrors.DuplicateActiveCashFlowType);
            }

            // Step 5: Validate configuration using specialized validator
            Result configurationValidation = configurationValidator.Validate(
                command.CashFlowType,
                command.Configuration);

            if (configurationValidation.IsFailure)
            {
                logger.LogWarning(
                    "Configuration validation failed for {CashFlowType}: {Error}",
                    command.CashFlowType, configurationValidation.Error.Description);
                return Result.Failure<SaveFacilityCashFlowTypeResponse>(
                    configurationValidation.Error);
            }

            // Step 6: Create and save entity
#pragma warning disable CA1869 // Cache and reuse 'JsonSerializerOptions' instances
            string configurationJson = JsonSerializer.Serialize(
                command.Configuration,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
#pragma warning restore CA1869 // Cache and reuse 'JsonSerializerOptions' instances

            var facilityCashFlowType = new FacilityCashFlowType
            {
                Id = Guid.CreateVersion7(),
                FacilityNumber = command.FacilityNumber,
                SegmentId = command.SegmentId,
                ScenarioId = command.ScenarioId,
                CashFlowType = command.CashFlowType,
                Configuration = configurationJson,
                IsActive = true,
                CreatedBy = userContext.UserId,
                CreatedAt = dateTimeProvider.UtcNow,
                UpdatedAt = dateTimeProvider.UtcNow
            };

            context.FacilityCashFlowTypes.Add(facilityCashFlowType);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Created cash flow type {CashFlowType} for facility {FacilityNumber}, scenario {ScenarioId}",
                command.CashFlowType, command.FacilityNumber, command.ScenarioId);

            // Step 7: Get user name for response
            var user = await context.Users
                .AsNoTracking()
                .Where(u => u.Id == userContext.UserId)
                .Select(u => new { u.FirstName, u.LastName })
                .FirstOrDefaultAsync(cancellationToken);

            string createdByName = user != null
                ? $"{user.FirstName} {user.LastName}"
                : "Unknown User";

            var response = new SaveFacilityCashFlowTypeResponse
            {
                Id = facilityCashFlowType.Id,
                FacilityNumber = facilityCashFlowType.FacilityNumber,
                SegmentId = segment.Id,
                SegmentName = segment.Name,
                ScenarioId = scenario.Id,
                ScenarioName = scenario.ScenarioName,
                CashFlowType = facilityCashFlowType.CashFlowType,
                CashFlowTypeName = CashFlowTypeNames.GetName(facilityCashFlowType.CashFlowType),
                Configuration = command.Configuration,
                IsActive = facilityCashFlowType.IsActive,
                CreatedAt = facilityCashFlowType.CreatedAt,
                CreatedBy = facilityCashFlowType.CreatedBy,
                CreatedByName = createdByName
            };

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error saving cash flow type for facility {FacilityNumber}",
                command.FacilityNumber);

            return Result.Failure<SaveFacilityCashFlowTypeResponse>(
                Error.Failure(
                    "FacilityCashFlowType.SaveError",
                    $"An error occurred while saving the cash flow type: {ex.Message}"));
        }
    }
}
