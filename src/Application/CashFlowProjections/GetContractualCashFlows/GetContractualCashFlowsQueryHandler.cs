using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using SharedKernel;

namespace Application.CashFlowProjections.GetContractualCashFlows;

/// <summary>
/// Handler to retrieve and calculate contractual cash flows from portfolio snapshot
/// </summary>
internal sealed class GetContractualCashFlowsQueryHandler(
    IApplicationDbContext context,
    ILogger<GetContractualCashFlowsQueryHandler> logger)
    : IQueryHandler<GetContractualCashFlowsQuery, ContractualCashFlowsResponse>
{
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

            // Get facility details from latest portfolio snapshot
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

            // Calculate tenure in months
            int tenureMonths = (maturityDate.Year - DateTime.UtcNow.Year) * 12 +
                               maturityDate.Month - DateTime.UtcNow.Month;

            if (tenureMonths <= 0)
            {
                logger.LogWarning("Facility {FacilityNumber} has already matured", query.FacilityNumber);
                tenureMonths = 1; // Minimum 1 month
            }

            // Generate cash flow projections based on installment type
            List<MonthlyCashFlow> cashFlows = GenerateCashFlowProjections(
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
            decimal principalAmount;

            // Calculate based on installment type
            if (installmentType.Contains("Equal", StringComparison.OrdinalIgnoreCase))
            {
                // Equal Monthly Installment (EMI)
                decimal emi = CalculateEMI(totalOutstanding, monthlyInterestRate, tenureMonths);
                principalAmount = emi - interestAmount;
            }
            else if (installmentType.Contains("Bullet", StringComparison.OrdinalIgnoreCase))
            {
                // Bullet payment - principal only in last month
                principalAmount = month == tenureMonths ? remainingPrincipal : 0;
            }
            else
            {
                // Default: Equal principal repayment
                principalAmount = totalOutstanding / tenureMonths;
            }

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
