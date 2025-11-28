namespace Application.Abstractions.Parsing;

/// <summary>
/// Represents a parsed cash flow entry from Excel file
/// </summary>
public sealed record ParsedCashFlow(int Month, decimal CashFlow);
