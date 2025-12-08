namespace Saral.FileProcessor.Core.Services;

public sealed class FileProcessingService(
    IFileLoader fileLoader,
    IValidationConfigurationService validationConfigurationService,
    IMultiFileValidator multiFileValidator) : IFileProcessingService
{
    private readonly IFileLoader _fileLoader = fileLoader ?? throw new ArgumentNullException(nameof(fileLoader));
    private readonly IValidationConfigurationService _validationConfigurationService = validationConfigurationService ?? throw new ArgumentNullException(nameof(validationConfigurationService));
    private readonly IMultiFileValidator _multiFileValidator = multiFileValidator ?? throw new ArgumentNullException(nameof(multiFileValidator));

    public async Task<AnalysisResult> ProcessFileAsync(string filePath, AnalysisOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Load the file
            FileLoadContext loadContext = _fileLoader.Load(filePath);

            return ProcessLoadedData(loadContext, options, cancellationToken);
        }, cancellationToken);
    }

    public async Task<AnalysisResult> ProcessFileAsync(Stream fileStream, string fileName, AnalysisOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Load from stream (this will require FileLoader enhancement)
            FileLoadContext loadContext = _fileLoader.Load(fileStream, fileName);

            return ProcessLoadedData(loadContext, options, cancellationToken);
        }, cancellationToken);
    }

    private AnalysisResult ProcessLoadedData(FileLoadContext loadContext, AnalysisOptions? options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Configure validation using ReadOnlySpan for better performance
        string[] columnNames = loadContext.Data.ColumnKeys.ToArray();
        IDataValidator dataValidator = _validationConfigurationService.ConfigureValidation(columnNames.AsSpan());

        // Create analyzer and process
        var analyzer = new DataQualityAnalyzer(dataValidator);
        AnalysisOptions analysisOptions = options ?? new AnalysisOptions(SampleRowLimit: 10, ExcelRowLimit: 10_000);

        cancellationToken.ThrowIfCancellationRequested();

        return analyzer.Analyze(loadContext, analysisOptions);
    }

    //private AnalysisResult ProcessLoadedDataWithoutValidation(FileLoadContext loadContext, AnalysisOptions? options, CancellationToken cancellationToken)
    //{
    //    cancellationToken.ThrowIfCancellationRequested();

    //    // Create analyzer without validation (for basic analysis only)
    //    var analyzer = new DataQualityAnalyzer(null); // No validation
    //    var analysisOptions = options ?? new AnalysisOptions(SampleRowLimit: 10, ExcelRowLimit: 10_000);

    //    cancellationToken.ThrowIfCancellationRequested();

    //    return analyzer.Analyze(loadContext, analysisOptions);
    //}

    public async Task<MultiFileAnalysisResult> ProcessMultipleFilesAsync(
        IEnumerable<string> filePaths,
        AnalysisOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePaths);

        string[] filePathArray = filePaths.ToArray();
        if (filePathArray.Length == 0)
        {
            throw new ArgumentException("At least one file path must be provided.", nameof(filePaths));
        }

        DateTimeOffset startTime = DateTimeOffset.UtcNow;

        // Load and analyze each file individually first
        IEnumerable<Task<IndividualFileResult>> loadTasks = filePathArray.Select((filePath, index) =>
            Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                FileLoadContext loadContext = _fileLoader.Load(filePath);

                // Perform full analysis including individual validation for each file
                AnalysisOptions analysisOptions = options ?? new AnalysisOptions(SampleRowLimit: 10, ExcelRowLimit: 10_000);
                AnalysisResult result = ProcessLoadedData(loadContext, analysisOptions, cancellationToken);

                return new IndividualFileResult
                {
                    FileName = Path.GetFileName(filePath),
                    Analysis = result,
                    FileIndex = index
                };
            }, cancellationToken));

        IndividualFileResult[] individualResults = await Task.WhenAll(loadTasks);

        // Now perform comprehensive cross-file validation
        CrossFileValidation crossFileValidation = await _multiFileValidator.ValidateAcrossFilesAsync(individualResults, cancellationToken);

        // Create consolidated analysis
        ConsolidatedAnalysis consolidatedAnalysis = CreateConsolidatedAnalysis(individualResults, crossFileValidation);

        TimeSpan processingTime = DateTimeOffset.UtcNow - startTime;

        return new MultiFileAnalysisResult
        {
            IndividualResults = individualResults,
            ConsolidatedAnalysis = consolidatedAnalysis,
            CrossFileValidation = crossFileValidation,
            SummaryStatistics = CreateMultiFileSummaryStatistics(individualResults, crossFileValidation, processingTime)
        };
    }

    public async Task<MultiFileAnalysisResult> ProcessMultipleFilesAsync(
        IEnumerable<FileStreamInfo> fileStreams,
        AnalysisOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStreams);

        FileStreamInfo[] fileStreamArray = fileStreams.ToArray();

        if (fileStreamArray.Length == 0)
        {
            throw new ArgumentException("At least one file stream must be provided.", nameof(fileStreams));
        }

        DateTimeOffset startTime = DateTimeOffset.UtcNow;

        // Load all files first (without validation)
        IEnumerable<Task<FileLoadInfo>> loadTasks = fileStreamArray.Select((fileInfo, index) =>
            Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                FileLoadContext loadContext = _fileLoader.Load(fileInfo.Stream, fileInfo.FileName);
                return new FileLoadInfo
                {
                    FileName = fileInfo.FileName,
                    LoadContext = loadContext,
                    FileIndex = index
                };
            }, cancellationToken));

        FileLoadInfo[] loadedFiles = await Task.WhenAll(loadTasks);

        // Analyze each file individually first to create IndividualFileResult[]
        IEnumerable<Task<IndividualFileResult>> individualResultTasks = loadedFiles.Select(fileInfo =>
            Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Configure validation for this file
                string[] columnNames = fileInfo.LoadContext.Data.ColumnKeys.ToArray();
                IDataValidator dataValidator = _validationConfigurationService.ConfigureValidation(columnNames.AsSpan());
                var analyzer = new DataQualityAnalyzer(dataValidator);
                AnalysisOptions analysisOptions = options ?? new AnalysisOptions(SampleRowLimit: 10, ExcelRowLimit: 10_000);

                AnalysisResult analysis = analyzer.Analyze(fileInfo.LoadContext, analysisOptions);

                return new IndividualFileResult
                {
                    FileName = fileInfo.FileName,
                    Analysis = analysis,
                    FileIndex = fileInfo.FileIndex
                };
            }, cancellationToken));

        IndividualFileResult[] individualResults = await Task.WhenAll(individualResultTasks);

        // Perform cross-file validation on the individual results
        CrossFileValidation crossFileValidation = await _multiFileValidator.ValidateAcrossFilesAsync(individualResults, cancellationToken);

        TimeSpan processingTime = DateTimeOffset.UtcNow - startTime;

        // Create consolidated analysis from individual results
        ConsolidatedAnalysis consolidatedAnalysis = CreateConsolidatedAnalysis(individualResults, crossFileValidation);

        return new MultiFileAnalysisResult
        {
            IndividualResults = individualResults,
            ConsolidatedAnalysis = consolidatedAnalysis,
            CrossFileValidation = crossFileValidation,
            SummaryStatistics = CreateMultiFileSummaryStatistics(individualResults, crossFileValidation, processingTime)
        };
    }

    private static ConsolidatedAnalysis CreateConsolidatedAnalysis(
        IndividualFileResult[] individualResults,
        CrossFileValidation crossFileValidation)
    {
        int totalRows = individualResults.Sum(r => r.Analysis.TotalRows);
        //int totalValidRows = individualResults.Sum(r => r.Analysis.ValidationSummary?.ValidRows ?? 0);
        //int totalInvalidRows = individualResults.Sum(r => r.Analysis.ValidationSummary?.InvalidRows ?? 0);

        // Merge column metrics across files
        var allColumnMetrics = individualResults
            .SelectMany(r => r.Analysis.Columns)
            .GroupBy(cm => cm.Name)
            .ToDictionary(g => g.Key, g => MergeColumnMetrics(g.ToArray()));

        //int crossFileErrorCount = crossFileValidation.CrossFileValidationErrors.Count +
        //                        crossFileValidation.ConditionalValidationErrors.Count +
        //                        crossFileValidation.DependentValidationErrors.Count +
        //                        crossFileValidation.TotalDuplicateRows;

        ImmutableArray<RowValidation> consolidatedRowValidations = ImmutableArray<RowValidation>.Empty; // Create empty for now
        var validationSummary = new ValidationSummary(
            RowValidations: consolidatedRowValidations,
            ModifiedData: null
        );

        return new ConsolidatedAnalysis
        {
            TotalRows = totalRows,
            TotalColumns = individualResults[0].Analysis.Columns.Length,
            ValidationSummary = validationSummary,
            ColumnMetrics = allColumnMetrics,
            CrossFileErrors = crossFileValidation.CrossFileValidationErrors
        };
    }

    private static ColumnMetrics MergeColumnMetrics(ColumnMetrics[] columnMetrics)
    {
        ColumnMetrics first = columnMetrics[0];

        return first with
        {
            MissingCount = columnMetrics.Sum(cm => cm.MissingCount),
            UniqueValues = columnMetrics.Sum(cm => cm.UniqueValues),
            MissingPercentage = columnMetrics.Average(cm => cm.MissingPercentage)
        };
    }

    private static double CalculateValidationRate(int validRows, int invalidRows, int crossFileErrors)
    {
        int totalRows = validRows + invalidRows;
        return totalRows == 0 ? 0.0 : (double)(validRows - crossFileErrors) / totalRows * 100.0;
    }

    private static MultiFileSummaryStatistics CreateMultiFileSummaryStatistics(
        IndividualFileResult[] individualResults,
        CrossFileValidation crossFileValidation,
        TimeSpan processingTime)
    {
        int totalRows = individualResults.Sum(r => r.Analysis.TotalRows);
        int totalValidRows = individualResults.Sum(r => r.Analysis.ValidationSummary?.ValidRows ?? 0);
        int totalInvalidRows = individualResults.Sum(r => r.Analysis.ValidationSummary?.InvalidRows ?? 0);

        int crossFileErrorCount = crossFileValidation.CrossFileValidationErrors.Count +
                                crossFileValidation.ConditionalValidationErrors.Count +
                                crossFileValidation.DependentValidationErrors.Count +
                                crossFileValidation.TotalDuplicateRows;

        return new MultiFileSummaryStatistics
        {
            TotalFiles = individualResults.Length,
            TotalRows = totalRows,
            ValidRows = totalValidRows - crossFileErrorCount,
            InvalidRows = totalInvalidRows + crossFileErrorCount,
            CrossFileValidationErrors = crossFileErrorCount,
            DataQualityScore = CalculateValidationRate(totalValidRows, totalInvalidRows, crossFileErrorCount),
            ProcessingTime = processingTime
        };
    }
}
