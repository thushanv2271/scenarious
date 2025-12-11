using Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for loan details queries
/// Centralizes all raw SQL queries for loan_details table
/// Uses DISTINCT ON to handle potential duplicates
/// </summary>
internal sealed class LoanDetailsRepository : ILoanDetailsRepository
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<LoanDetailsRepository> _logger;

    // SQL query constants - FIXED to handle duplicates using DISTINCT ON
    private const string FacilityCollateralQuery = @"
        SELECT DISTINCT ON (facility_number, product_category) 
            customer_number, 
            facility_number, 
            collateral_type, 
            collateral_value
        FROM loan_details
        WHERE facility_number = @facilityNumber
        ORDER BY facility_number, product_category, period DESC";

    private const string CustomerFacilitiesQuery = @"
        SELECT DISTINCT ON (facility_number, product_category)
            customer_number, 
            facility_number, 
            product_category, 
            segment, 
            branch,
            total_os,
            interest_rate,
            grant_date,
            maturity_date,
            days_past_due,
            bucket_label
        FROM loan_details
        WHERE customer_number = @customerNumber
        ORDER BY facility_number, product_category, period DESC";

    private const string FacilityBasicDetailsQuery = @"
        SELECT DISTINCT ON (facility_number, product_category)
            customer_number, 
            facility_number, 
            product_category, 
            segment
        FROM loan_details
        WHERE facility_number = @facilityNumber
        ORDER BY facility_number, product_category, period DESC";

    private const string FacilityLoanDetailsQuery = @"
        SELECT DISTINCT ON (facility_number, product_category)
            customer_number, 
            facility_number, 
            total_os, 
            interest_rate,
            grant_date, 
            maturity_date, 
            installment_type
        FROM loan_details
        WHERE facility_number = @facilityNumber
        ORDER BY facility_number, product_category, period DESC";

    public LoanDetailsRepository(
        IApplicationDbContext context,
        ILogger<LoanDetailsRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets facility collateral information (latest snapshot)
    /// </summary>
    public async Task<FacilityCollateralDetail?> GetFacilityCollateralAsync(
        string facilityNumber,
        CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(FacilityCollateralQuery, connection);
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

    /// <summary>
    /// Gets all facilities for a customer (latest snapshot per facility)
    /// </summary>
    public async Task<List<CustomerFacilityDetail>> GetCustomerFacilitiesAsync(
        string customerNumber,
        CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(CustomerFacilitiesQuery, connection);
        command.Parameters.AddWithValue("@customerNumber", customerNumber);

        var facilities = new List<CustomerFacilityDetail>();
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            facilities.Add(new CustomerFacilityDetail
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

        return facilities;
    }

    /// <summary>
    /// Gets basic facility details for validation (latest snapshot)
    /// </summary>
    public async Task<FacilityBasicDetail?> GetFacilityBasicDetailsAsync(
        string facilityNumber,
        CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(FacilityBasicDetailsQuery, connection);
        command.Parameters.AddWithValue("@facilityNumber", facilityNumber);

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return new FacilityBasicDetail
            {
                CustomerNumber = reader.GetString(0),
                FacilityNumber = reader.GetString(1),
                ProductCategory = reader.GetString(2),
                Segment = reader.GetString(3)
            };
        }

        return null;
    }

    /// <summary>
    /// Gets complete loan details for a facility (latest snapshot)
    /// </summary>
    public async Task<FacilityLoanDetail?> GetFacilityLoanDetailsAsync(
        string facilityNumber,
        CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await CreateConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(FacilityLoanDetailsQuery, connection);
        command.Parameters.AddWithValue("@facilityNumber", facilityNumber);

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return new FacilityLoanDetail
            {
                CustomerNumber = reader.GetString(0),
                FacilityNumber = reader.GetString(1),
                TotalOutstanding = reader.GetDecimal(2),
                InterestRate = reader.GetDecimal(3),
                GrantDate = reader.GetDateTime(4),
                MaturityDate = reader.GetDateTime(5),
                InstallmentType = reader.GetString(6)
            };
        }

        return null;
    }

    /// <summary>
    /// Creates and opens a database connection
    /// Throws InvalidOperationException if connection string is missing
    /// </summary>
    private async Task<NpgsqlConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        var dbContext = _context as DbContext;
        string? connectionString = dbContext?.Database.GetConnectionString();

        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogError("Database connection string not found");
            throw new InvalidOperationException("Database connection string not found");
        }

        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
