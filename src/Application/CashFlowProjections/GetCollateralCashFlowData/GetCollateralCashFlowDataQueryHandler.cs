using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using SharedKernel;

namespace Application.CashFlowProjections.GetCollateralCashFlowData;

/// <summary>
/// Handler to retrieve collateral value and last quarter cash flow data
/// </summary>
internal sealed class GetCollateralCashFlowDataQueryHandler(
    IApplicationDbContext context,
    ILogger<GetCollateralCashFlowDataQueryHandler> logger)
    : IQueryHandler<GetCollateralCashFlowDataQuery, CollateralCashFlowDataResponse>
{
    public async Task<Result<CollateralCashFlowDataResponse>> Handle(
        GetCollateralCashFlowDataQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Get collateral data from loan_details
            FacilityCollateralDetail? collateralData = await GetCollateralDataAsync(query.FacilityNumber, cancellationToken);
            if (collateralData == null)
            {
                return Result.Failure<CollateralCashFlowDataResponse>(
                    Error.NotFound("Facility.NotFound",
                        $"Facility {query.FacilityNumber} not found"));
            }

            // Step 2: Check for uploaded last quarter cash flow file
            // This looks for scenarios with uploaded files for this facility's segment
            LastQuarterCashFlowData? lastQuarterData = await GetLastQuarterCashFlowDataAsync(
                query.FacilityNumber,
                cancellationToken);

            var response = new CollateralCashFlowDataResponse
            {
                FacilityNumber = collateralData.FacilityNumber,
                CustomerNumber = collateralData.CustomerNumber,
                Collateral = new CollateralData
                {
                    CollateralType = collateralData.CollateralType,
                    CollateralValue = collateralData.CollateralValue,
                    HaircutPercentage = 0.40m,
                    NetRealizableValue = collateralData.CollateralValue * (1 - 0.40m)
                },
                LastQuarterCashFlows = lastQuarterData
            };

            logger.LogInformation(
                "Retrieved collateral data for facility {FacilityNumber}. Collateral Value: {Value}",
                query.FacilityNumber, collateralData.CollateralValue);

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error retrieving collateral/cash flow data for facility {FacilityNumber}",
                query.FacilityNumber);
            return Result.Failure<CollateralCashFlowDataResponse>(
                Error.Failure("CollateralCashFlowData.RetrievalError",
                    $"Error retrieving data: {ex.Message}"));
        }
    }

    private async Task<FacilityCollateralDetail?> GetCollateralDataAsync(
        string facilityNumber,
        CancellationToken cancellationToken)
    {
        var dbContext = context as DbContext;
        string? connectionString = dbContext?.Database.GetConnectionString();

        if (string.IsNullOrEmpty(connectionString))
        {
            return null;
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        string sql = @"
            SELECT 
                customer_number,
                facility_number,
                collateral_type,
                collateral_value
            FROM loan_details
            WHERE facility_number = @facilityNumber
            ORDER BY period DESC
            LIMIT 1";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@facilityNumber", facilityNumber);

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return new FacilityCollateralDetail
            {
                CustomerNumber = reader.GetString(0),
                FacilityNumber = reader.GetString(1),
                CollateralType = reader.GetString(2),
                CollateralValue = reader.GetDecimal(3)
            };
        }

        return null;
    }

    private async Task<LastQuarterCashFlowData?> GetLastQuarterCashFlowDataAsync(
        string facilityNumber,
        CancellationToken cancellationToken)
    {
        // Find scenarios with uploaded files for this facility's segment
        var uploadedFile = await (
            from fcf in context.FacilityCashFlowTypes
            join scenario in context.Scenarios on fcf.ScenarioId equals scenario.Id
            join file in context.UploadedFiles on scenario.UploadedFileId equals file.Id
            where fcf.FacilityNumber == facilityNumber
                  && fcf.CashFlowType == Domain.FacilityCashFlowTypes.CashFlowsType.LastQuarterCashFlows
                  && fcf.IsActive
            orderby file.UploadedAt descending
            select new
            {
                file.Id,
                file.OriginalFileName,
                file.UploadedAt
            }
        ).FirstOrDefaultAsync(cancellationToken);

        if (uploadedFile == null)
        {
            return null;
        }

        // Note: Actual cash flow parsing from uploaded file would happen here
        // For now, return placeholder data
        return new LastQuarterCashFlowData
        {
            UploadedFileId = uploadedFile.Id,
            FileName = uploadedFile.OriginalFileName,
            UploadedAt = uploadedFile.UploadedAt.DateTime,
            CashFlows = new List<HistoricalCashFlow>
            {
                // Placeholder - would parse from actual file
                new HistoricalCashFlow
                {
                    Date = DateTime.UtcNow.AddMonths(-3),
                    Amount = 0,
                    Description = "Data from uploaded file - parsing not implemented"
                }
            }
        };
    }

    private sealed class FacilityCollateralDetail
    {
        public string CustomerNumber { get; init; } = string.Empty;
        public string FacilityNumber { get; init; } = string.Empty;
        public string CollateralType { get; init; } = string.Empty;
        public decimal CollateralValue { get; init; }
    }
}
