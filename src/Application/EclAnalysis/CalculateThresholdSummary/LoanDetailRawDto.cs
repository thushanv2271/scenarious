namespace Application.EclAnalysis.CalculateThresholdSummary;

/// <summary>
/// DTO for mapping raw SQL query results from loan_details table
/// </summary>
public sealed class LoanDetailRawDto
{
    public string CustomerNumber { get; set; } = string.Empty;
    public decimal TotalOs { get; set; } //Total Outstanding Balance
}
