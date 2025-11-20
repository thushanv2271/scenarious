using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using SharedKernel;

namespace Application.CashFlowProjections.GetCustomerFacilities;

/// <summary>
/// Handler to retrieve all facilities for a customer from loan_details
/// </summary>
internal sealed class GetCustomerFacilitiesQueryHandler(
    IApplicationDbContext context,
    ILogger<GetCustomerFacilitiesQueryHandler> logger)
    : IQueryHandler<GetCustomerFacilitiesQuery, List<CustomerFacilityResponse>>
{
    public async Task<Result<List<CustomerFacilityResponse>>> Handle(
        GetCustomerFacilitiesQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var dbContext = context as DbContext;
            string? connectionString = dbContext?.Database.GetConnectionString();

            if (string.IsNullOrEmpty(connectionString))
            {
                logger.LogError("Database connection string not found");
                return Result.Failure<List<CustomerFacilityResponse>>(
                    Error.Failure("Database.ConnectionError", "Database connection string not found"));
            }

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            // Query to get all facilities for the customer
            // Group by facility to aggregate if multiple records exist
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
                    MAX(days_past_due) as days_past_due,
                    MAX(bucket_label) as bucket_label
                FROM loan_details
                WHERE customer_number = @customerNumber
                GROUP BY customer_number, facility_number, product_category, segment, branch
                ORDER BY facility_number";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@customerNumber", query.CustomerNumber);

            var facilities = new List<CustomerFacilityResponse>();

            await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                facilities.Add(new CustomerFacilityResponse
                {
                    CustomerNumber = reader.GetString(0),
                    FacilityNumber = reader.GetString(1),
                    ProductCategory = reader.GetString(2),
                    Segment = reader.GetString(3),
                    Branch = reader.GetString(4),
                    TotalOutstanding = reader.GetDecimal(5),
                    InterestRate = reader.GetDecimal(6),
                    GrantDate = reader.GetDateTime(7),
                    MaturityDate = reader.GetDateTime(8),
                    DaysPastDue = reader.GetInt32(9),
                    BucketLabel = reader.GetString(10)
                });
            }

            if (!facilities.Any())
            {
                logger.LogWarning("No facilities found for customer: {CustomerNumber}", query.CustomerNumber);
                return Result.Failure<List<CustomerFacilityResponse>>(
                    Error.NotFound("CustomerFacilities.NotFound",
                        $"No facilities found for customer {query.CustomerNumber}"));
            }

            logger.LogInformation("Found {Count} facilities for customer {CustomerNumber}",
                facilities.Count, query.CustomerNumber);

            return Result.Success(facilities);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving facilities for customer {CustomerNumber}",
                query.CustomerNumber);
            return Result.Failure<List<CustomerFacilityResponse>>(
                Error.Failure("CustomerFacilities.RetrievalError",
                    $"Error retrieving facilities: {ex.Message}"));
        }
    }
}
