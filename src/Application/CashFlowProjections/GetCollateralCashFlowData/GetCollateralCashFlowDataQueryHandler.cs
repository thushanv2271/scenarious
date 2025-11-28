using System.Text.Json;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Parsing;
using Application.CashFlowProjections.Common;
using Application.FacilityCashFlowTypes.SaveCashFlowType;
using Domain.FacilityCashFlowTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.CashFlowProjections.GetCollateralCashFlowData;

/// <summary>
/// Handler to retrieve collateral value and last quarter cash flow data
/// Uses repository pattern to separate data access concerns
/// </summary>
internal sealed class GetCollateralCashFlowDataQueryHandler(
    ILoanDetailsRepository loanRepository,
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
            // Step 1: Get collateral data from repository
            FacilityCollateralDetail? collateralData = await loanRepository
                .GetFacilityCollateralAsync(query.FacilityNumber, cancellationToken);

            if (collateralData == null)
            {
                return Result.Failure<CollateralCashFlowDataResponse>(
                    Error.NotFound("Facility.NotFound",
                        $"Facility {query.FacilityNumber} not found"));
            }

            // Step 2: Get saved haircut percentage or use default
            decimal haircutPercentage = await GetHaircutPercentageAsync(
                query.FacilityNumber, cancellationToken);

            // Step 3: Get last quarter cash flow data
            LastQuarterCashFlowData? lastQuarterData = await GetLastQuarterCashFlowDataAsync(
                query.FacilityNumber, cancellationToken);

            // Step 4: Build response
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
                "Retrieved collateral data for facility {FacilityNumber}. Value: {Value}, Haircut: {Haircut}%",
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

    /// <summary>
    /// Gets configured haircut percentage or returns default
    /// </summary>
    private async Task<decimal> GetHaircutPercentageAsync(
        string facilityNumber,
        CancellationToken cancellationToken)
    {
        FacilityCashFlowType? savedConfig = await context.FacilityCashFlowTypes
            .AsNoTracking()
            .Where(f => f.FacilityNumber == facilityNumber &&
                       f.CashFlowType == CashFlowsType.CollateralRealization &&
                       f.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (savedConfig == null)
        {
            return CashFlowConstants.DefaultHaircutPercentage;
        }

        try
        {
            CashFlowConfigurationDto? config = JsonSerializer.Deserialize<CashFlowConfigurationDto>(
                savedConfig.Configuration, JsonOptions);

            return config?.HaircutPercentage ?? CashFlowConstants.DefaultHaircutPercentage;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to parse saved configuration for facility {FacilityNumber}, using default haircut",
                facilityNumber);
            return CashFlowConstants.DefaultHaircutPercentage;
        }
    }

    /// <summary>
    /// Gets last quarter cash flow data from uploaded files
    /// </summary>
    private async Task<LastQuarterCashFlowData?> GetLastQuarterCashFlowDataAsync(
        string facilityNumber,
        CancellationToken cancellationToken)
    {
        // Single query with navigation properties to avoid N+1
        var uploadedFileInfo = await (
            from fcf in context.FacilityCashFlowTypes
            join scenario in context.Scenarios on fcf.ScenarioId equals scenario.Id
            join file in context.UploadedFiles on scenario.UploadedFileId equals file.Id
            where fcf.FacilityNumber == facilityNumber
                  && fcf.CashFlowType == CashFlowsType.LastQuarterCashFlows
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

        // Parse cash flows from Excel file
        Result<List<ParsedCashFlow>> parseResult = await excelParser.ParseCashFlowsAsync(
            uploadedFileInfo.PhysicalPath, cancellationToken);

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
