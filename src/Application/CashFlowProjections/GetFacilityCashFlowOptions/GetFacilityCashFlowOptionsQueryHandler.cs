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
/// </summary>
internal sealed class GetFacilityCashFlowOptionsQueryHandler(
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
            logger.LogInformation("Starting GetFacilityCashFlowOptions for facility: {FacilityNumber}", query.FacilityNumber);

            // Step 1: Get facility details from loan_details
            logger.LogInformation("Step 1: Fetching facility details from loan_details");
            FacilityDetail? facilityDetails = await GetFacilityDetailsAsync(query.FacilityNumber, cancellationToken);

            if (facilityDetails == null)
            {
                logger.LogWarning("Step 1 Failed: Facility not found in loan_details: {FacilityNumber}", query.FacilityNumber);
                return Result.Failure<FacilityCashFlowOptionsResponse>(
                    Error.NotFound("Facility.NotFound",
                        $"Facility {query.FacilityNumber} not found in loan_details"));
            }

            logger.LogInformation("Step 1 Success: Found facility - Customer: {CustomerNumber}, Segment: {Segment}",
                facilityDetails.CustomerNumber, facilityDetails.Segment);

            // Step 2: Find matching segment in master data
            logger.LogInformation("Step 2: Finding segment in master data for: {Segment}", facilityDetails.Segment);

            Domain.Segments.Segment? segment = await context.Segments
                .AsNoTracking()
                .FirstOrDefaultAsync(s =>
                    s.Name.Equals(facilityDetails.Segment, StringComparison.OrdinalIgnoreCase),
                    cancellationToken);

            if (segment == null)
            {
                logger.LogWarning("Step 2 Failed: Segment not found in master data: {Segment}", facilityDetails.Segment);
                return Result.Failure<FacilityCashFlowOptionsResponse>(
                    Error.NotFound("Segment.NotFound",
                        $"Segment '{facilityDetails.Segment}' not found in master data"));
            }

            logger.LogInformation("Step 2 Success: Found segment - ID: {SegmentId}, Name: {SegmentName}",
                segment.Id, segment.Name);

            // Step 3: Get scenarios for this segment
            logger.LogInformation("Step 3: Fetching scenarios for segment ID: {SegmentId}", segment.Id);

            List<Domain.Scenarios.Scenario> scenarios = await context.Scenarios
                .AsNoTracking()
                .Where(s => s.SegmentId == segment.Id)
                .OrderBy(s => s.ScenarioName)
                .ToListAsync(cancellationToken);

            if (!scenarios.Any())
            {
                logger.LogWarning("Step 3 Failed: No scenarios found for segment: {SegmentName}", segment.Name);
                return Result.Failure<FacilityCashFlowOptionsResponse>(
                    Error.NotFound("Scenarios.NotFound",
                        $"No scenarios configured for segment '{segment.Name}'"));
            }

            logger.LogInformation("Step 3 Success: Found {Count} scenarios", scenarios.Count);

            // Step 4: Get saved cash flow configurations for this facility
            logger.LogInformation("Step 4: Fetching saved cash flow configurations for facility: {FacilityNumber}",
                query.FacilityNumber);

            List<Domain.FacilityCashFlowTypes.FacilityCashFlowType> savedConfigurations;

#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
            try
            {
                savedConfigurations = await context.FacilityCashFlowTypes
                    .AsNoTracking()
                    .Where(f => f.FacilityNumber == query.FacilityNumber && f.IsActive)
                    .ToListAsync(cancellationToken);

                logger.LogInformation("Step 4 Success: Found {Count} saved configurations", savedConfigurations.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Step 4 Failed: Error querying facility_cash_flow_types table");
                throw;
            }
#pragma warning restore S2139 // Exceptions should be either logged or rethrown but not both

            // Step 5: Get user names for created by
            logger.LogInformation("Step 5: Fetching user details");

            var userIds = savedConfigurations.Select(c => c.CreatedBy).Distinct().ToList();

            if (userIds.Any())
            {
                logger.LogInformation("Step 5: Fetching {Count} user records", userIds.Count);
            }

            var users = await context.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FirstName, u.LastName })
                .ToListAsync(cancellationToken);

            var userDictionary = users.ToDictionary(
                u => u.Id,
                u => $"{u.FirstName} {u.LastName}");

            logger.LogInformation("Step 5 Success: Retrieved {Count} user names", users.Count);

            // Step 6: Build response
            logger.LogInformation("Step 6: Building response with {ScenarioCount} scenarios", scenarios.Count);

            var scenarioResponses = new List<ScenarioOptionResponse>();

            foreach (Domain.Scenarios.Scenario scenario in scenarios)
            {
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
                try
                {
                    FacilityCashFlowType? savedConfig = savedConfigurations.FirstOrDefault(c => c.ScenarioId == scenario.Id);

                    CashFlowTypeConfigurationResponse? configResponse = null;

                    if (savedConfig != null)
                    {
                        logger.LogDebug("Processing saved config for scenario {ScenarioName}, Type: {CashFlowType}",
                            scenario.ScenarioName, savedConfig.CashFlowType);

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
                            logger.LogError(jsonEx, "Error deserializing configuration JSON for config ID: {ConfigId}", savedConfig.Id);
                            // Continue with null configuration
                        }
                    }

                    scenarioResponses.Add(new ScenarioOptionResponse
                    {
                        ScenarioId = scenario.Id,
                        ScenarioName = scenario.ScenarioName,
                        Probability = scenario.Probability,
                        ContractualCashFlowsEnabled = scenario.ContractualCashFlowsEnabled,
                        LastQuarterCashFlowsEnabled = scenario.LastQuarterCashFlowsEnabled,
                        OtherCashFlowsEnabled = scenario.OtherCashFlowsEnabled,
                        CollateralValueEnabled = scenario.CollateralValueEnabled,
                        SavedCashFlowConfiguration = configResponse
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing scenario: {ScenarioName}", scenario.ScenarioName);
                    throw;
                }
#pragma warning restore S2139 // Exceptions should be either logged or rethrown but not both
            }

            logger.LogInformation("Step 6 Success: Built {Count} scenario responses", scenarioResponses.Count);

            var response = new FacilityCashFlowOptionsResponse
            {
                FacilityNumber = facilityDetails.FacilityNumber,
                CustomerNumber = facilityDetails.CustomerNumber,
                ProductCategory = facilityDetails.ProductCategory,
                Segment = facilityDetails.Segment,
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
                Error.Failure("Database.Error",
                    $"Database error: {npgEx.Message}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Unexpected error retrieving cash flow options for facility {FacilityNumber}. Exception Type: {ExceptionType}",
                query.FacilityNumber, ex.GetType().Name);
            return Result.Failure<FacilityCashFlowOptionsResponse>(
                Error.Failure("FacilityCashFlowOptions.RetrievalError",
                    $"Error retrieving cash flow options: {ex.Message}"));
        }
    }

    private async Task<FacilityDetail?> GetFacilityDetailsAsync(
        string facilityNumber,
        CancellationToken cancellationToken)
    {
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
        try
        {
            var dbContext = context as DbContext;
            string? connectionString = dbContext?.Database.GetConnectionString();

            if (string.IsNullOrEmpty(connectionString))
            {
                logger.LogError("Database connection string is null or empty");
                return null;
            }

            logger.LogDebug("Opening database connection for facility lookup");

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            string sql = @"
                SELECT 
                    customer_number,
                    facility_number,
                    product_category,
                    segment
                FROM loan_details
                WHERE facility_number = @facilityNumber
                LIMIT 1";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@facilityNumber", facilityNumber);

            logger.LogDebug("Executing SQL query for facility: {FacilityNumber}", facilityNumber);

            await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                var detail = new FacilityDetail
                {
                    CustomerNumber = reader.GetString(0),
                    FacilityNumber = reader.GetString(1),
                    ProductCategory = reader.GetString(2),
                    Segment = reader.GetString(3)
                };

                logger.LogDebug("Successfully read facility details from database");
                return detail;
            }

            logger.LogDebug("No facility found with number: {FacilityNumber}", facilityNumber);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetFacilityDetailsAsync for facility: {FacilityNumber}", facilityNumber);
            throw;
        }
#pragma warning restore S2139 // Exceptions should be either logged or rethrown but not both
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

    private sealed class FacilityDetail
    {
        public string CustomerNumber { get; init; } = string.Empty;
        public string FacilityNumber { get; init; } = string.Empty;
        public string ProductCategory { get; init; } = string.Empty;
        public string Segment { get; init; } = string.Empty;
    }
}
