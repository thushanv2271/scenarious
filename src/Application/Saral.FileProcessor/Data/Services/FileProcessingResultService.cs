namespace Saral.FileProcessor.Data.Services;

public class FileProcessingResultService : IFileProcessingResultService
{
    private readonly FileProcessorDbContext _context;

    public FileProcessingResultService(FileProcessorDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<int> SaveFileValidationResultAsync(
        AnalysisResult analysisResult, 
        string filename, 
        Guid sessionId, 
        string? timePeriod = null,
        string? collectiveImpairmentType = null,
        CancellationToken cancellationToken = default)
    {
        var entity = new FileValidationResult
        {
            Filename = filename,
            TotalRows = analysisResult.TotalRows,
            TotalErrors = analysisResult.ValidationSummary?.RowValidations.Count(r => !r.IsValid) ?? 0,
            Status = DetermineStatus(analysisResult),
            SessionId = sessionId,
            TimePeriod = timePeriod,
            CollectiveImpairmentType = collectiveImpairmentType,
            CreatedOnUtc = DateTime.UtcNow
        };

        _context.FileValidationResults.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        
        return entity.Id;
    }

    public async Task<List<int>> SaveMultiFileProcessingResultAsync(
        MultiFileAnalysisResult analysisResult, 
        string sessionId,
        string? timePeriod = null,
        string? collectiveImpairmentType = null,
        CancellationToken cancellationToken = default)
    {
        var fileResultIds = new List<int>();
        
        // Save individual file results with session tracking
        foreach (IndividualFileResult individualResult in analysisResult.IndividualResults)
        {
            var sessionGuid = Guid.Parse(sessionId);
            int fileResultId = await SaveFileValidationResultAsync(
                individualResult.Analysis, 
                individualResult.FileName, 
                sessionGuid,
                timePeriod,
                collectiveImpairmentType,
                cancellationToken);
            fileResultIds.Add(fileResultId);
        }
        
        return fileResultIds;
    }

    public async Task<IEnumerable<FileValidationResult>> GetFileValidationResultsAsync(string filenamePattern, CancellationToken cancellationToken = default)
    {
        return await _context.FileValidationResults
            .Where(fvr => EF.Functions.Like(fvr.Filename, filenamePattern))
            .OrderByDescending(fvr => fvr.CreatedOnUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FileValidationResult>> GetSessionResultsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        // For now, we'll use a simple approach - in a real implementation, you might add a SessionId column
        // or use a separate table to link files to sessions
        return await _context.FileValidationResults
            .Where(fvr => fvr.Filename.Contains(sessionId) || fvr.CreatedOnUtc >= DateTime.UtcNow.AddHours(-1))
            .OrderByDescending(fvr => fvr.CreatedOnUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<FileValidationResult>> GetRecentFileValidationResultsAsync(int count = 50, CancellationToken cancellationToken = default)
    {
        return await _context.FileValidationResults
            .OrderByDescending(fvr => fvr.CreatedOnUtc)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    private static string DetermineStatus(AnalysisResult analysisResult)
    {
        if (analysisResult.ValidationSummary?.RowValidations.IsDefaultOrEmpty != false)
        {
            return "ProcessingFailed";
        }

        bool hasErrors = analysisResult.ValidationSummary.RowValidations.Any(r => !r.IsValid);
        return hasErrors ? "HasValidationErrors" : "Valid";
    }
}
