namespace Saral.FileProcessor.Core.Models;

public sealed record AnalysisOptions(int SampleRowLimit = 10, int ExcelRowLimit = 10_000);