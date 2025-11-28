using System.Text.Json;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.FacilityCashFlowTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using SharedKernel;

namespace Application.CashFlowProjections.GetFacilityCashFlowOptions;

/// <summary>
/// Handler to retrieve scenarios and cash flow options for a facility
/// Fixed N+1 query issue by eager loading all related data
/// </summary>
internal sealed class GetFacilityCashFlowOptionsQueryHandler(
    ILoanDetailsRepository loanRepository,
    IApplicationDbContext context,
    ILogger<GetFacilityCashFlowOptionsQueryHandler> logger)
    : IQueryHandler<GetFacilityCashFlowOptionsQuery, FacilityCashFlowOptionsResponse>
{
    public async Task<Result<FacilityCashFlowOptionsResponse>> Handle(
        GetFacilityCashFlowOptionsQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Starting GetFacilityCashFlowOptions for facility: {FacilityNumber}",
                query.FacilityNumber);

            // Step 1: Get facility details from repository
            FacilityBasicDetail? facilityDetails = await loanRepository
                .GetFacilityBasicDetailsAsync(query.FacilityNumber, cancellationToken);

            if (facilityDetails == null)
            {
                logger.LogWarning("Facility not found: {FacilityNumber}", query.FacilityNumber);
                return Result.Failure<FacilityCashFlowOptionsResponse>(
                    Error.NotFound("Facility.NotFound",
                        $"Facility {query.FacilityNumber} not found in loan_details"));
            }

            // Step 2: Find matching segment (with product category)
            // Use database-side case-insensitive comparison (Postgres ILIKE) to avoid client-side functions
            string segmentName = facilityDetails.Segment ?? string.Empty;

            Domain.Segments.Segment? segment = await context.Segments
                .AsNoTracking()
                .Include(s => s.ProductCategory)  // Eager load to avoid N+1
                .FirstOrDefaultAsync(s => EF.Functions.ILike(s.Name, segmentName),
                    cancellationToken);

            if (segment == null)
            {
                logger.LogWarning("Segment not found: {Segment}", facilityDetails.Segment);
                return Result.Failure<FacilityCashFlowOptionsResponse>(
                    Error.NotFound("Segment.NotFound",
                        $"Segment '{facilityDetails.Segment}' not found in master data"));
            }

            // Step 3: Get all scenarios with related data in ONE query (fixed N+1)
            List<Domain.Scenarios.Scenario> scenarios = await context.Scenarios
                .AsNoTracking()
                .Include(s => s.UploadedFile)  // Eager load uploaded file
                .Where(s => s.SegmentId == segment.Id)
                .OrderBy(s => s.ScenarioName)
                .ToListAsync(cancellationToken);

            if (!scenarios.Any())
            {
                logger.LogWarning("No scenarios found for segment: {SegmentName}", segment.Name);
                return Result.Failure<FacilityCashFlowOptionsResponse>(
                    Error.NotFound("Scenarios.NotFound",
                        $"No scenarios configured for segment '{segment.Name}'"));
            }

            // Step 4: Get ALL saved configurations in ONE query (fixed N+1)
            List<FacilityCashFlowType> savedConfigurations = await context.FacilityCashFlowTypes
                .AsNoTracking()
                .Where(f => f.FacilityNumber == query.FacilityNumber && f.IsActive)
                .ToListAsync(cancellationToken);

            // Step 5: Get ALL users in ONE query (fixed N+1)
            var userIds = savedConfigurations.Select(c => c.CreatedBy).Distinct().ToList();
            Dictionary<Guid, string> userDictionary = await context.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, FullName = u.FirstName + " " + u.LastName })
                .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

            // Step 6: Build response (all data already loaded)
            var scenarioResponses = scenarios.Select(scenario =>
            {
                FacilityCashFlowType? savedConfig = savedConfigurations
                    .FirstOrDefault(c => c.ScenarioId == scenario.Id);

                CashFlowTypeConfigurationResponse? configResponse = null;

                if (savedConfig != null)
                {
                    try
                    {
                        object? configObject = JsonSerializer.Deserialize<object>(savedConfig.Configuration);

                        configResponse = new CashFlowTypeConfigurationResponse
                        {
                            Id = savedConfig.Id,
                            CashFlowType = savedConfig.CashFlowType,
                            CashFlowTypeName = GetCashFlowTypeName(savedConfig.CashFlowType),
                            Configuration = configObject ?? new(),
                            CreatedAt = savedConfig.CreatedAt,
                            CreatedByName = userDictionary.GetValueOrDefault(savedConfig.CreatedBy, "Unknown User")
                        };
                    }
                    catch (JsonException jsonEx)
                    {
                        logger.LogError(jsonEx, "Error deserializing configuration for ID: {ConfigId}",
                            savedConfig.Id);
                    }
                }

                return new ScenarioOptionResponse
                {
                    ScenarioId = scenario.Id,
                    ScenarioName = scenario.ScenarioName,
                    Probability = scenario.Probability,
                    ContractualCashFlowsEnabled = scenario.ContractualCashFlowsEnabled,
                    LastQuarterCashFlowsEnabled = scenario.LastQuarterCashFlowsEnabled,
                    OtherCashFlowsEnabled = scenario.OtherCashFlowsEnabled,
                    CollateralValueEnabled = scenario.CollateralValueEnabled,
                    SavedCashFlowConfiguration = configResponse
                };
            }).ToList();

            var response = new FacilityCashFlowOptionsResponse
            {
                FacilityNumber = facilityDetails.FacilityNumber,
                CustomerNumber = facilityDetails.CustomerNumber,
                ProductCategory = facilityDetails.ProductCategory,
                Segment = facilityDetails.Segment ?? string.Empty,
                SegmentId = segment.Id,
                AvailableScenarios = scenarioResponses
            };

            logger.LogInformation(
                "Successfully retrieved cash flow options for facility {FacilityNumber} with {ScenarioCount} scenarios",
                query.FacilityNumber, scenarioResponses.Count);

            return Result.Success(response);
        }
        catch (NpgsqlException npgEx)
        {
            logger.LogError(npgEx,
                "Database error retrieving cash flow options for facility {FacilityNumber}. Error Code: {ErrorCode}",
                query.FacilityNumber, npgEx.ErrorCode);
            return Result.Failure<FacilityCashFlowOptionsResponse>(
                Error.Failure("Database.Error", $"Database error: {npgEx.Message}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error retrieving cash flow options for facility {FacilityNumber}",
                query.FacilityNumber);
            return Result.Failure<FacilityCashFlowOptionsResponse>(
                Error.Failure("FacilityCashFlowOptions.RetrievalError",
                    $"Error retrieving cash flow options: {ex.Message}"));
        }
    }

    /// <summary>
    /// Gets user-friendly name for cash flow type
    /// </summary>
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
}
