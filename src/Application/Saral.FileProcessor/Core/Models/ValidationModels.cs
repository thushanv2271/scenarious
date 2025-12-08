namespace Saral.FileProcessor.Core.Models;

public sealed record ValidationResult(
    bool IsValid,
    string? ErrorMessage = null
);

public sealed record RowValidation(
    int RowIndex,
    Dictionary<string, ValidationResult> ColumnValidations
)
{
    public bool IsValid => ColumnValidations.Values.All(v => v.IsValid);
    public string? ValidationMessage => IsValid ? null : 
        string.Join(" & ", ColumnValidations.Where(kv => !kv.Value.IsValid)
            .Select(kv => kv.Value.ErrorMessage)
            .Where(msg => !string.IsNullOrEmpty(msg)));
}

public sealed record ValidationSummary(
    ImmutableArray<RowValidation> RowValidations,
    Frame<int, string>? ModifiedData = null
)
{
    public int TotalRows => RowValidations.Length;
    public int ValidRows => RowValidations.Count(r => r.IsValid);
    public int InvalidRows => RowValidations.Count(r => !r.IsValid);
    public double ValidationSuccessRate => TotalRows == 0 ? 100.0 : (double)ValidRows / TotalRows * 100.0;
}