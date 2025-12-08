using Application.Abstractions.Messaging;
using SharedKernel;
using Saral.FileProcessor.Core.Services;
using Saral.FileProcessor.Reports.Services;

namespace Application.Files.ProcessFile;

internal sealed class ProcessFileCommandHandler(
    IFileProcessingService fileProcessingService,
    IReportGenerationService reportGenerationService)
    : ICommandHandler<ProcessFileCommand, ProcessFileResponse>
{
    public async Task<Result<ProcessFileResponse>> Handle(
        ProcessFileCommand command, 
        CancellationToken cancellationToken)
    {
        try
        {
            using var stream = new MemoryStream(command.Content);
            
            // Call your Saral.FileProcessor service with correct parameters
            dynamic analysisResult = await fileProcessingService.ProcessFileAsync(
                stream, 
                command.FileName, 
                null, 
                cancellationToken);

            // Generate reports using your report service
            ReportPaths reportPaths = await reportGenerationService.GenerateAllReportsAsync(
                analysisResult, 
                null, 
                cancellationToken);

            var response = new ProcessFileResponse(
                AnalysisResult: analysisResult,
                ReportPaths: reportPaths
            );

            return Result.Success(response);
        }
        catch (Exception ex)
        {
            return Result.Failure<ProcessFileResponse>(new Error(
                "FileProcessing.Failed",
                $"An error occurred while processing the file: {ex.Message}",
                ErrorType.Problem
            ));
        }
    }
}