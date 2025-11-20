using System.Text.Json;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Abstractions.Parsing;
using Application.FacilityCashFlowTypes.SaveCashFlowType;
using Domain.FacilityCashFlowTypes;
using Domain.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using SharedKernel;

namespace Application.CashFlowProjections.GetContractualCashFlows;

internal sealed class GetContractualCashFlowsQueryHandler(
    IApplicationDbContext context,
    IExcelCashFlowParser excelParser,
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
            var dbContext = context as DbContext;
            string? connectionString = dbContext?.Database.GetConnectionString();

            if (string.IsNullOrEmpty(connectionString))
            {
                logger.LogError("Database connection string not found");
                return Result.Failure<ContractualCashFlowsResponse>(
                    Error.Failure("Database.ConnectionError", "Database connection string not found"));
            }

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            string sql = @"
                SELECT 
                    customer_number,
                    facility_number,
                    total_os,
                    interest_rate,
                    grant_date,
                    maturity_date,
                    installment_type
                FROM loan_details
                WHERE facility_number = @facilityNumber
                ORDER BY period DESC
                LIMIT 1";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@facilityNumber", query.FacilityNumber);

            await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                logger.LogWarning("Facility not found: {FacilityNumber}", query.FacilityNumber);
                return Result.Failure<ContractualCashFlowsResponse>(
                    Error.NotFound("Facility.NotFound",
                        $"Facility {query.FacilityNumber} not found in portfolio snapshot"));
            }

            string customerNumber = reader.GetString(0);
            string facilityNumber = reader.GetString(1);
            decimal totalOutstanding = reader.GetDecimal(2);
            decimal interestRate = reader.GetDecimal(3);
            DateTime grantDate = reader.GetDateTime(4);
            DateTime maturityDate = reader.GetDateTime(5);
            string installmentType = reader.GetString(6);

            int tenureMonths = CalculateTenureMonths(maturityDate);

            FacilityCashFlowType? savedConfig = await context.FacilityCashFlowTypes
                .AsNoTracking()
                .Where(f => f.FacilityNumber == query.FacilityNumber &&
                           (f.CashFlowType == Domain.FacilityCashFlowTypes.CashFlowsType.ContractualCashFlows ||
                            f.CashFlowType == Domain.FacilityCashFlowTypes.CashFlowsType.ContractModification) &&
                           f.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            List<MonthlyCashFlow> cashFlows;

            if (savedConfig != null)
            {
                Result<List<MonthlyCashFlow>>? uploadedCashFlows = await TryGetUploadedCashFlowsAsync(
                    savedConfig, query.FacilityNumber, cancellationToken);

                if (uploadedCashFlows != null)
                {
                    return Result.Success(new ContractualCashFlowsResponse
                    {
                        FacilityNumber = facilityNumber,
                        CustomerNumber = customerNumber,
                        AmortisedCost = totalOutstanding,
                        InterestRate = interestRate,
                        GrantDate = grantDate,
                        MaturityDate = maturityDate,
                        TenureMonths = tenureMonths,
                        InstallmentType = installmentType,
                        ProjectedCashFlows = uploadedCashFlows.Value
                    });
                }
            }

            cashFlows = GenerateCashFlowProjections(
                totalOutstanding,
                interestRate,
                tenureMonths,
                installmentType,
                DateTime.UtcNow);

            var response = new ContractualCashFlowsResponse
            {
                FacilityNumber = facilityNumber,
                CustomerNumber = customerNumber,
                AmortisedCost = totalOutstanding,
                InterestRate = interestRate,
                GrantDate = grantDate,
                MaturityDate = maturityDate,
                TenureMonths = tenureMonths,
                InstallmentType = installmentType,
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

    private int CalculateTenureMonths(DateTime maturityDate)
    {
        int tenureMonths = (maturityDate.Year - DateTime.UtcNow.Year) * 12 +
                           maturityDate.Month - DateTime.UtcNow.Month;

        if (tenureMonths <= 0)
        {
            logger.LogWarning("Facility has already matured, using minimum tenure");
            return 1;
        }

        return tenureMonths;
    }

    private async Task<Result<List<MonthlyCashFlow>>?> TryGetUploadedCashFlowsAsync(
        FacilityCashFlowType savedConfig,
        string facilityNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            CashFlowConfigurationDto? config = JsonSerializer.Deserialize<CashFlowConfigurationDto>(
                savedConfig.Configuration,
                JsonOptions);

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
                uploadedFile.PhysicalPath,
                cancellationToken);

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
                "Failed to use uploaded payment schedule for facility {FacilityNumber}, falling back to calculation",
                facilityNumber);
            return null;
        }
    }

    private static List<MonthlyCashFlow> GenerateCashFlowProjections(
        decimal totalOutstanding,
        decimal annualInterestRate,
        int tenureMonths,
        string installmentType,
        DateTime startDate)
    {
        var cashFlows = new List<MonthlyCashFlow>();
        decimal monthlyInterestRate = annualInterestRate / 100 / 12;
        decimal remainingPrincipal = totalOutstanding;

        for (int month = 1; month <= tenureMonths; month++)
        {
            decimal interestAmount = remainingPrincipal * monthlyInterestRate;
            decimal principalAmount = CalculatePrincipalAmount(
                totalOutstanding,
                remainingPrincipal,
                monthlyInterestRate,
                tenureMonths,
                installmentType,
                month);

            decimal totalAmount = principalAmount + interestAmount;
            remainingPrincipal -= principalAmount;

            cashFlows.Add(new MonthlyCashFlow
            {
                Month = month,
                PrincipalAmount = Math.Round(principalAmount, 2),
                InterestAmount = Math.Round(interestAmount, 2),
                TotalAmount = Math.Round(totalAmount, 2),
                PaymentDate = startDate.AddMonths(month)
            });
        }

        return cashFlows;
    }

    private static decimal CalculatePrincipalAmount(
        decimal totalOutstanding,
        decimal remainingPrincipal,
        decimal monthlyInterestRate,
        int tenureMonths,
        string installmentType,
        int currentMonth)
    {
        if (installmentType.Contains("Equal", StringComparison.OrdinalIgnoreCase))
        {
            decimal emi = CalculateEMI(totalOutstanding, monthlyInterestRate, tenureMonths);
            decimal interestAmount = remainingPrincipal * monthlyInterestRate;
            return emi - interestAmount;
        }

        if (installmentType.Contains("Bullet", StringComparison.OrdinalIgnoreCase))
        {
            return currentMonth == tenureMonths ? remainingPrincipal : 0;
        }

        return totalOutstanding / tenureMonths;
    }

    private static decimal CalculateEMI(decimal principal, decimal monthlyRate, int months)
    {
        if (monthlyRate == 0)
        {
            return principal / months;
        }

        decimal numerator = principal * monthlyRate * (decimal)Math.Pow((double)(1 + monthlyRate), months);
        decimal denominator = (decimal)Math.Pow((double)(1 + monthlyRate), months) - 1;

        return numerator / denominator;
    }
}
