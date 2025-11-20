using SharedKernel;

namespace Application.Abstractions.Parsing;

public interface IExcelCashFlowParser
{
    Task<Result<List<ParsedCashFlow>>> ParseCashFlowsAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}


