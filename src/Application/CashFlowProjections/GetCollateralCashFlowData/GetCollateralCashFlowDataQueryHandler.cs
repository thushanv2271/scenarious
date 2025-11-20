using System.Text.Json;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Parsing;
using Application.FacilityCashFlowTypes.SaveCashFlowType;
using Domain.FacilityCashFlowTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using SharedKernel;

namespace Application.CashFlowProjections.GetCollateralCashFlowData;

internal sealed class GetCollateralCashFlowDataQueryHandler(
    IApplicationDbContext context,
    IExcelCashFlowParser excelParser,
    ILogger<GetCollateralCashFlowDataQueryHandler> logger)
    : IQueryHandler<GetCollateralCashFlowDataQuery, CollateralCashFlowDataResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<Result<CollateralCashFlowDataResponse>> Handle(
        GetCollateralCashFlowDataQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            FacilityCollateralDetail? collateralData = await GetCollateralDataAsync(
                query.FacilityNumber, cancellationToken);

            if (collateralData == null)
            {
                return Result.Failure<CollateralCashFlowDataResponse>(
                    Error.NotFound("Facility.NotFound",
                        $"Facility {query.FacilityNumber} not found"));
            }

            FacilityCashFlowType? savedConfig = await context.FacilityCashFlowTypes
                .AsNoTracking()
                .Where(f => f.FacilityNumber == query.FacilityNumber &&
                           f.CashFlowType == Domain.FacilityCashFlowTypes.CashFlowsType.CollateralRealization &&
                           f.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            decimal haircutPercentage = 0.40m;

            if (savedConfig != null)
            {
                try
                {
                    CashFlowConfigurationDto? config = JsonSerializer.Deserialize<CashFlowConfigurationDto>(
                        savedConfig.Configuration,
                        JsonOptions);

                    if (config?.HaircutPercentage.HasValue == true)
                    {
                        haircutPercentage = config.HaircutPercentage.Value;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to parse saved configuration for facility {FacilityNumber}",
                        query.FacilityNumber);
                }
            }

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
                    HaircutPercentage = haircutPercentage,
                    NetRealizableValue = collateralData.CollateralValue * (1 - haircutPercentage)
                },
                LastQuarterCashFlows = lastQuarterData
            };

            logger.LogInformation(
                "Retrieved collateral data for facility {FacilityNumber}. Collateral Value: {Value}, Haircut: {Haircut}%",
                query.FacilityNumber, collateralData.CollateralValue, haircutPercentage * 100);

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
        var uploadedFileInfo = await (
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
                file.UploadedAt,
                file.PhysicalPath
            }
        ).FirstOrDefaultAsync(cancellationToken);

        if (uploadedFileInfo == null)
        {
            return null;
        }

        Result<List<ParsedCashFlow>> parseResult = await excelParser.ParseCashFlowsAsync(
            uploadedFileInfo.PhysicalPath,
            cancellationToken);

        if (parseResult.IsFailure)
        {
            logger.LogWarning("Failed to parse cash flows from file {FileName}: {Error}",
                uploadedFileInfo.OriginalFileName, parseResult.Error.Description);

            return new LastQuarterCashFlowData
            {
                UploadedFileId = uploadedFileInfo.Id,
                FileName = uploadedFileInfo.OriginalFileName,
                UploadedAt = uploadedFileInfo.UploadedAt.DateTime,
                CashFlows = new List<HistoricalCashFlow>()
            };
        }

        return new LastQuarterCashFlowData
        {
            UploadedFileId = uploadedFileInfo.Id,
            FileName = uploadedFileInfo.OriginalFileName,
            UploadedAt = uploadedFileInfo.UploadedAt.DateTime,
            CashFlows = parseResult.Value.Select(cf => new HistoricalCashFlow
            {
                Date = DateTime.UtcNow.AddMonths(-cf.Month),
                Amount = cf.CashFlow
            }).ToList()
        };
    }


}
