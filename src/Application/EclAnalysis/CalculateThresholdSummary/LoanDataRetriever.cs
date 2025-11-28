// src/Application/EclAnalysis/CalculateThresholdSummary/LoanDataRetriever.cs
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace Application.EclAnalysis.CalculateThresholdSummary;

/// <summary>
/// Retrieves loan data from the database for threshold calculations
/// </summary>
internal static class LoanDataRetriever
{
    /// <summary>
    /// Retrieves customer exposure data for a specific branch
    /// </summary>
    public static async Task<List<LoanDetailRawDto>> RetrieveAsync(
        string connectionString,
        string branchName,
        CancellationToken cancellationToken)
    {
        var customerExposures = new List<LoanDetailRawDto>();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // Get total outstanding balance per customer in the branch
        // Sorted in descending order for efficient processing
        string sql = @"
            SELECT 
                customer_number,
                SUM(total_os) as total_os
            FROM loan_details
            WHERE branch = @branchName
            GROUP BY customer_number
            ORDER BY total_os DESC";

        await using var sqlCommand = new NpgsqlCommand(sql, connection);
        sqlCommand.Parameters.AddWithValue("@branchName", branchName);

        await using NpgsqlDataReader reader = await sqlCommand.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            customerExposures.Add(new LoanDetailRawDto
            {
                CustomerNumber = reader.GetString(0),
                TotalOs = reader.GetDecimal(1)
            });
        }

        return customerExposures;
    }
}
