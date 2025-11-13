using Application.Models;
using Application.DTOs.PD;
using SharedKernel;
using SummaryModels = Application.Models.PDSummary;

namespace Application.Abstractions.Calculations;

/// <summary>
/// Interface for PD Calculation service
/// </summary>
public interface IPDCalculationService
{
    /// <summary>
    /// Executes step 1 of PD calculation: file extraction, calculation, and DB insertion
    /// </summary>
    /// <param name="quarterEndedDates">Dictionary of file identifiers to quarter ended dates</param>
    /// <param name="datePassedDueBuckets">List of date passed due bucket configurations</param>
    /// <param name="finalBucketPayload">Final bucket calculation configuration</param>
    /// <param name="createdBy">User who initiated the calculation</param>
    /// <param name="type">Optional type to determine file path suffix (Yearly=Y, Quarterly=Q, Monthly=M). If not provided, uses full path from appsettings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the operation</returns>
    Task<Result> ExecuteStep1Async(
        Dictionary<string, DateTime> quarterEndedDates,
        List<DatePassedDueBucket> datePassedDueBuckets,
        FinalBucketPayload finalBucketPayload,
        string createdBy,
        string? type = null,
        CancellationToken cancellationToken = default);




    /// <summary>
    /// Executes step 2 of PD calculation: migration matrix generation with counts and percentages
    /// </summary>
    /// <param name="createdBy">User who initiated the calculation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the generated migration matrices</returns>
    Task<Result<IReadOnlyList<PeriodMigrationMatrix>>> ExecuteStep2Async(
        TimeConfig timeConfig,
        List<DatePassedDueBucket> datePassedDueBuckets,
        List<PDConfiguration> pdConfiguration,
        string createdBy,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// Executes step 3 of PD calculation: Generate PD Summary tables from migration matrices
    /// </summary>
    /// <param name="migrationData">Migration matrices response object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing PD summary table JSON</returns>
    Task<Result<string>> ExecuteStep3Async(
        SummaryModels.MigrationMatricesAndSummaryResponse migrationData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes step 4 of PD calculation: Generate PD extrapolation tables for all three methods
    /// </summary>
    /// <param name="createdBy">User who initiated the calculation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the PD extrapolation results with all three methods</returns>
    Task<Result<PdExtrapolationResultDto>> ExecuteStep4Async(
        AveragePDTablesResponse? step3Data,
        string createdBy,
        CancellationToken cancellationToken = default);
}
