using System.Text.Json;
using Application.Abstractions.Calculations;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Parsing;
using Application.FacilityCashFlowTypes.SaveCashFlowType;
using Domain.FacilityCashFlowTypes;
using Domain.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.CashFlowProjections.GetContractualCashFlows;

/// <summary>
/// Handler to retrieve contractual cash flows for a facility
/// Delegates calculations to ICashFlowCalculationService
/// </summary>
internal sealed class GetContractualCashFlowsQueryHandler(
    ILoanDetailsRepository loanRepository,
    IApplicationDbContext context,
    IExcelCashFlowParser excelParser,
    ICashFlowCalculationService calculationService,
    ILogger<GetContractualCashFlowsQueryHandler> logger)
    : IQueryHandler<GetContractualCashFlowsQuery, ContractualCashFlowsResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<Result<ContractualCashFlowsResponse>> Handle(
        GetContractualCashFlowsQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Get loan details from repository
            FacilityLoanDetail? loanDetail = await loanRepository
                .GetFacilityLoanDetailsAsync(query.FacilityNumber, cancellationToken);

            if (loanDetail == null)
            {
                logger.LogWarning("Facility not found: {FacilityNumber}", query.FacilityNumber);
                return Result.Failure<ContractualCashFlowsResponse>(
                    Error.NotFound("Facility.NotFound",
                        $"Facility {query.FacilityNumber} not found in portfolio snapshot"));
            }

            // Step 2: Calculate tenure
            int tenureMonths = calculationService.CalculateTenureMonths(loanDetail.MaturityDate);

            // Step 3: Try to get uploaded payment schedule
            Result<List<MonthlyCashFlow>>? uploadedCashFlows = await TryGetUploadedCashFlowsAsync(
                query.FacilityNumber, cancellationToken);

            List<MonthlyCashFlow> cashFlows;
            if (uploadedCashFlows != null && uploadedCashFlows.IsSuccess)
            {
                cashFlows = uploadedCashFlows.Value;
            }
            else
            {
                // Step 4: Generate cash flows using calculation service
                cashFlows = calculationService.GenerateCashFlowProjections(
                    loanDetail.TotalOutstanding,
                    loanDetail.InterestRate,
                    tenureMonths,
                    loanDetail.InstallmentType,
                    DateTime.UtcNow);
            }

            var response = new ContractualCashFlowsResponse
            {
                FacilityNumber = loanDetail.FacilityNumber,
                CustomerNumber = loanDetail.CustomerNumber,
                AmortisedCost = loanDetail.TotalOutstanding,
                InterestRate = loanDetail.InterestRate,
                GrantDate = loanDetail.GrantDate,
                MaturityDate = loanDetail.MaturityDate,
                TenureMonths = tenureMonths,
                InstallmentType = loanDetail.InstallmentType,
                ProjectedCashFlows = cashFlows
            };

            logger.LogInformation(
                "Generated {Count} monthly cash flows for facility {FacilityNumber}",
                cashFlows.Count, query.FacilityNumber);

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error retrieving contractual cash flows for facility {FacilityNumber}",
                query.FacilityNumber);
            return Result.Failure<ContractualCashFlowsResponse>(
                Error.Failure("ContractualCashFlows.RetrievalError",
                    $"Error retrieving contractual cash flows: {ex.Message}"));
        }
    }

    /// <summary>
    /// Attempts to get uploaded payment schedule
    /// </summary>
    private async Task<Result<List<MonthlyCashFlow>>?> TryGetUploadedCashFlowsAsync(
        string facilityNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            FacilityCashFlowType? savedConfig = await context.FacilityCashFlowTypes
                .AsNoTracking()
                .Where(f => f.FacilityNumber == facilityNumber &&
                           (f.CashFlowType == CashFlowsType.ContractualCashFlows ||
                            f.CashFlowType == CashFlowsType.ContractModification) &&
                           f.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (savedConfig == null)
            {
                return null;
            }

            CashFlowConfigurationDto? config = JsonSerializer.Deserialize<CashFlowConfigurationDto>(
                savedConfig.Configuration, JsonOptions);

            if (config?.UploadedFileId == null)
            {
                return null;
            }

            UploadedFile? uploadedFile = await context.UploadedFiles
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == config.UploadedFileId.Value, cancellationToken);

            if (uploadedFile == null)
            {
                return null;
            }

            Result<List<ParsedCashFlow>> parseResult = await excelParser.ParseCashFlowsAsync(
                uploadedFile.PhysicalPath, cancellationToken);

            if (parseResult.IsFailure)
            {
                return null;
            }

            var cashFlows = parseResult.Value.Select(cf => new MonthlyCashFlow
            {
                Month = cf.Month,
                PrincipalAmount = 0,
                InterestAmount = 0,
                TotalAmount = cf.CashFlow,
                PaymentDate = DateTime.UtcNow.AddMonths(cf.Month)
            }).ToList();

            logger.LogInformation(
                "Using uploaded payment schedule for facility {FacilityNumber}",
                facilityNumber);

            return Result.Success(cashFlows);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to use uploaded payment schedule for facility {FacilityNumber}",
                facilityNumber);
            return null;
        }
    }
}
