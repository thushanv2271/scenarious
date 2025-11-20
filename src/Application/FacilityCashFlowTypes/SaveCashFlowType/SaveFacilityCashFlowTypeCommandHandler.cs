using System.Text.Json;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.FacilityCashFlowTypes;
using Domain.Scenarios;
using Domain.Segments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using SharedKernel;

namespace Application.FacilityCashFlowTypes.SaveCashFlowType;

internal sealed class SaveFacilityCashFlowTypeCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider,
    ILogger<SaveFacilityCashFlowTypeCommandHandler> logger)
    : ICommandHandler<SaveFacilityCashFlowTypeCommand, SaveFacilityCashFlowTypeResponse>
{
    public async Task<Result<SaveFacilityCashFlowTypeResponse>> Handle(
        SaveFacilityCashFlowTypeCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            Segment? segment = await context.Segments
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == command.SegmentId, cancellationToken);

            if (segment is null)
            {
                logger.LogWarning("Segment not found: {SegmentId}", command.SegmentId);
                return Result.Failure<SaveFacilityCashFlowTypeResponse>(
                    FacilityCashFlowTypeErrors.SegmentNotFound(command.SegmentId));
            }

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
                    "Scenario {ScenarioId} is not linked to segment {SegmentId}",
                    command.ScenarioId, command.SegmentId);
                return Result.Failure<SaveFacilityCashFlowTypeResponse>(
                    FacilityCashFlowTypeErrors.ScenarioNotLinkedToSegment);
            }

            Result<LoanDetail> facilityValidationResult = await ValidateFacilityAsync(
                command.FacilityNumber,
                segment,
                cancellationToken);

            if (facilityValidationResult.IsFailure)
            {
                logger.LogWarning(
                    "Facility validation failed for {FacilityNumber}: {Error}",
                    command.FacilityNumber,
                    facilityValidationResult.Error.Description);
                return Result.Failure<SaveFacilityCashFlowTypeResponse>(
                    facilityValidationResult.Error);
            }

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

            Result configurationValidation = ValidateConfiguration(
                command.CashFlowType,
                command.Configuration);

            if (configurationValidation.IsFailure)
            {
                logger.LogWarning(
                    "Configuration validation failed for cash flow type {CashFlowType}: {Error}",
                    command.CashFlowType,
                    configurationValidation.Error.Description);
                return Result.Failure<SaveFacilityCashFlowTypeResponse>(
                    configurationValidation.Error);
            }

#pragma warning disable CA1869
            string configurationJson = JsonSerializer.Serialize(
                command.Configuration,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
#pragma warning restore CA1869

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
                CashFlowTypeName = GetCashFlowTypeName(facilityCashFlowType.CashFlowType),
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

    private async Task<Result<LoanDetail>> ValidateFacilityAsync(
        string facilityNumber,
        Segment segment,
        CancellationToken cancellationToken)
    {
        try
        {
            var dbContext = context as DbContext;
            string? connectionString = dbContext?.Database.GetConnectionString();

            if (string.IsNullOrEmpty(connectionString))
            {
                logger.LogError("Database connection string not found");
                return Result.Failure<LoanDetail>(
                    Error.Failure(
                        "Database.ConnectionError",
                        "Database connection string not found"));
            }

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            string sql = @"
                SELECT 
                    customer_number,
                    facility_number,
                    product_category,
                    segment,
                    branch,
                    SUM(total_os) as total_os,
                    MAX(interest_rate) as interest_rate,
                    MIN(grant_date) as grant_date,
                    MAX(maturity_date) as maturity_date,
                    MAX(days_past_due) as days_past_due
                FROM loan_details
                WHERE facility_number = @facilityNumber
                GROUP BY customer_number, facility_number, product_category, segment, branch
                LIMIT 1";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@facilityNumber", facilityNumber);

            await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                logger.LogWarning("Facility not found in loan_details: {FacilityNumber}", facilityNumber);
                return Result.Failure<LoanDetail>(
                    FacilityCashFlowTypeErrors.FacilityNotFound(facilityNumber));
            }

            var loanDetail = new LoanDetail
            {
                CustomerNumber = reader.GetString(0),
                FacilityNumber = reader.GetString(1),
                ProductCategory = reader.GetString(2),
                Segment = reader.GetString(3),
                Branch = reader.GetString(4),
                TotalOs = reader.GetDecimal(5),
                InterestRate = reader.GetDecimal(6),
                GrantDate = reader.GetDateTime(7),
                MaturityDate = reader.GetDateTime(8),
                DaysInArrears = reader.GetInt32(9)
            };

            if (!loanDetail.Segment.Equals(segment.Name, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning(
                    "Segment mismatch for facility {FacilityNumber}. Expected: {ExpectedSegment}, Found: {ActualSegment}",
                    facilityNumber, segment.Name, loanDetail.Segment);
                return Result.Failure<LoanDetail>(
                    FacilityCashFlowTypeErrors.FacilitySegmentMismatch);
            }

            logger.LogInformation(
                "Facility validated: {FacilityNumber}, Customer: {CustomerNumber}, Segment: {Segment}",
                loanDetail.FacilityNumber, loanDetail.CustomerNumber, loanDetail.Segment);

            return Result.Success(loanDetail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating facility {FacilityNumber}", facilityNumber);
            return Result.Failure<LoanDetail>(
                Error.Failure(
                    "FacilityCashFlowType.ValidationError",
                    $"Error validating facility: {ex.Message}"));
        }
    }

    private static Result ValidateConfiguration(
        CashFlowsType cashFlowType,
        CashFlowConfigurationDto configuration)
    {
        return cashFlowType switch
        {
            CashFlowsType.ContractualCashFlows =>
                Result.Success(),

            CashFlowsType.ContractModification =>
                ValidateContractModificationConfiguration(configuration),

            CashFlowsType.CollateralRealization =>
                ValidateCollateralConfiguration(configuration),

            CashFlowsType.LastQuarterCashFlows =>
                ValidateLastQuarterConfiguration(configuration),

            CashFlowsType.OtherCashFlows =>
                ValidateOtherCashFlowsConfiguration(configuration),

            _ => Result.Failure(
                FacilityCashFlowTypeErrors.InvalidConfiguration(
                    $"Unknown cash flow type: {cashFlowType}"))
        };
    }

    private static Result ValidateContractModificationConfiguration(
        CashFlowConfigurationDto configuration)
    {
        // Allow either parameter-based OR file-based configuration
        bool hasParameters = !string.IsNullOrWhiteSpace(configuration.Frequency) &&
                            configuration.Value.HasValue &&
                            configuration.TenureMonths.HasValue;

        bool hasFile = configuration.UploadedFileId.HasValue;

        if (!hasParameters && !hasFile)
        {
            return Result.Failure(
                FacilityCashFlowTypeErrors.InvalidConfiguration(
                    "Contract modification requires either payment parameters (frequency, value, tenure) or an uploaded payment schedule file"));
        }

        if (hasParameters)
        {
#pragma warning disable CS8629 // Nullable value type may be null.
            if (configuration.Value.Value <= 0)
            {
                return Result.Failure(
                    FacilityCashFlowTypeErrors.InvalidConfiguration(
                        "Value must be greater than zero"));
            }
#pragma warning restore CS8629 // Nullable value type may be null.

#pragma warning disable CS8629 // Nullable value type may be null.
            if (configuration.TenureMonths.Value <= 0)
            {
                return Result.Failure(
                    FacilityCashFlowTypeErrors.InvalidConfiguration(
                        "Tenure months must be greater than zero"));
            }
#pragma warning restore CS8629 // Nullable value type may be null.

            string[] validFrequencies = new[] { "Monthly", "Quarterly", "Annually" };
            if (!validFrequencies.Contains(configuration.Frequency, StringComparer.OrdinalIgnoreCase))
            {
                return Result.Failure(
                    FacilityCashFlowTypeErrors.InvalidConfiguration(
                        "Frequency must be Monthly, Quarterly, or Annually"));
            }
        }

        return Result.Success();
    }

    private static Result ValidateCollateralConfiguration(
        CashFlowConfigurationDto configuration)
    {
        if (!configuration.CollateralValue.HasValue || configuration.CollateralValue.Value <= 0)
        {
            return Result.Failure(
                FacilityCashFlowTypeErrors.InvalidConfiguration(
                    "Collateral value must be greater than zero"));
        }

        if (!configuration.RealizationMonth.HasValue || configuration.RealizationMonth.Value <= 0)
        {
            return Result.Failure(
                FacilityCashFlowTypeErrors.InvalidConfiguration(
                    "Realization month must be greater than zero"));
        }

        if (!configuration.HaircutPercentage.HasValue)
        {
            return Result.Failure(
                FacilityCashFlowTypeErrors.InvalidConfiguration(
                    "Haircut percentage is required"));
        }

        if (configuration.HaircutPercentage.Value < 0 || configuration.HaircutPercentage.Value > 1)
        {
            return Result.Failure(
                FacilityCashFlowTypeErrors.InvalidConfiguration(
                    "Haircut percentage must be between 0 and 1 (e.g., 0.40 for 40%)"));
        }

        return Result.Success();
    }

    private static Result ValidateLastQuarterConfiguration(
        CashFlowConfigurationDto configuration)
    {
        if (!configuration.UploadedFileId.HasValue)
        {
            return Result.Failure(
                FacilityCashFlowTypeErrors.InvalidConfiguration(
                    "Uploaded file ID is required for last quarter cash flows"));
        }

        return Result.Success();
    }

    private static Result ValidateOtherCashFlowsConfiguration(
        CashFlowConfigurationDto configuration)
    {
        if (configuration.CustomCashFlows == null || !configuration.CustomCashFlows.Any())
        {
            return Result.Failure(
                FacilityCashFlowTypeErrors.InvalidConfiguration(
                    "Custom cash flows are required for other cash flow type"));
        }

        foreach (CustomCashFlowDto cashFlow in configuration.CustomCashFlows)
        {
            if (cashFlow.Month <= 0)
            {
                return Result.Failure(
                    FacilityCashFlowTypeErrors.InvalidConfiguration(
                        "Month number must be greater than zero"));
            }

            if (cashFlow.Amount <= 0)
            {
                return Result.Failure(
                    FacilityCashFlowTypeErrors.InvalidConfiguration(
                        "Cash flow amount must be greater than zero"));
            }
        }

        return Result.Success();
    }

    private static string GetCashFlowTypeName(CashFlowsType cashFlowType)
    {
        return cashFlowType switch
        {
            CashFlowsType.ContractualCashFlows => "Contractual Cash Flows",
            CashFlowsType.ContractModification => "Contract Modification",
            CashFlowsType.CollateralRealization => "Collateral Realization",
            CashFlowsType.LastQuarterCashFlows => "Last Quarter Cash Flows",
            CashFlowsType.OtherCashFlows => "Other Cash Flows",
            _ => "Unknown"
        };
    }

    private sealed class LoanDetail
    {
        public string CustomerNumber { get; init; } = string.Empty;
        public string FacilityNumber { get; init; } = string.Empty;
        public string ProductCategory { get; init; } = string.Empty;
        public string Segment { get; init; } = string.Empty;
        public string Branch { get; init; } = string.Empty;
        public decimal TotalOs { get; init; }
        public decimal InterestRate { get; init; }
        public DateTime GrantDate { get; init; }
        public DateTime MaturityDate { get; init; }
        public int DaysInArrears { get; init; }
    }
}
