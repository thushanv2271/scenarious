using Saral.FileProcessor.Reports.Services;

namespace Application.Files.ProcessFile;

public sealed record ProcessFileResponse(
    object AnalysisResult,  // We'll use object for now until we know the exact type from your packages
    ReportPaths ReportPaths
);