using System.Text.Json;
using Application.Abstractions.Calculations;
using Application.Abstractions.Data;
using Application.Abstractions.Parsing;
using Application.CashFlowProjections.GetContractualCashFlows;
using Application.FacilityCashFlowTypes.SaveCashFlowType;
using Application.IndividualImpairment.DTOs;
using Application.IndividualImpairment.Services;
using Domain.FacilityCashFlowTypes;
using Domain.Files;
using Domain.Scenarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Infrastructure.Services;

/// <summary>
/// Orchestrates cash flow generation from various sources based on saved configurations
/// </summary>
internal sealed class CashFlowOrchestrationService : ICashFlowOrchestrationService
{
    private readonly IApplicationDbContext _context;
    private readonly ILoanDetailsRepository _loanRepository;
    private readonly IExcelCashFlowParser _excelParser;
    private readonly ICashFlowCalculationService _calculationService;
    private readonly ILogger<CashFlowOrchestrationService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CashFlowOrchestrationService(
        IApplicationDbContext context,
        ILoanDetailsRepository loanRepository,
        IExcelCashFlowParser excelParser,
        ICashFlowCalculationService calculationService,
        ILogger<CashFlowOrchestrationService> logger)
    {
        _context = context;
        _loanRepository = loanRepository;
        _excelParser = excelParser;
        _calculationService = calculationService;
        _logger = logger;
    }

    public async Task<Result<List<ScenarioCashFlowInput>>> BuildScenarioCashFlowsAsync(
        string facilityNumber,
        decimal interestRate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get facility loan details
            FacilityLoanDetail? loanDetail = await _loanRepository
                .GetFacilityLoanDetailsAsync(facilityNumber, cancellationToken);

            if (loanDetail == null)
            {
                return Result.Failure<List<ScenarioCashFlowInput>>(
                    Error.NotFound("Facility.NotFound",
                        $"Facility {facilityNumber} not found"));
            }

            // Get saved cash flow configurations WITH scenarios
            List<FacilityCashFlowType> cashFlowConfigs = await _context.FacilityCashFlowTypes
                .AsNoTracking()
                .Where(f => f.FacilityNumber == facilityNumber && f.IsActive)
                .ToListAsync(cancellationToken);

            if (!cashFlowConfigs.Any())
            {
                return Result.Failure<List<ScenarioCashFlowInput>>(
                    Error.NotFound("CashFlowConfig.NotFound",
                        $"No cash flow configurations found for facility {facilityNumber}"));
            }

            // Get unique scenario IDs
            var scenarioIds = cashFlowConfigs.Select(c => c.ScenarioId).Distinct().ToList();

            // Fetch scenarios separately
            Dictionary<Guid, Scenario> scenarios = await _context.Scenarios
                .AsNoTracking()
                .Where(s => scenarioIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, cancellationToken);

            var scenarioInputs = new List<ScenarioCashFlowInput>();

            // Group by scenario
            foreach (IGrouping<Guid, FacilityCashFlowType> scenarioGroup in cashFlowConfigs.GroupBy(c => c.ScenarioId))
            {
                if (!scenarios.TryGetValue(scenarioGroup.Key, out Scenario? scenario))
                {
                    _logger.LogWarning(
                        "Scenario not found for ScenarioId: {ScenarioId}",
                        scenarioGroup.Key);
                    continue;
                }

                var allCashFlows = new List<CashFlowItemInput>();

                _logger.LogDebug(
                    "Processing scenario {ScenarioName} with {ConfigCount} configurations",
                    scenario.ScenarioName, scenarioGroup.Count());

                // Process each cash flow type in this scenario
                foreach (FacilityCashFlowType cashFlowConfig in scenarioGroup)
                {
                    CashFlowConfigurationDto? configDto = null;
                    try
                    {
                        configDto = JsonSerializer.Deserialize<CashFlowConfigurationDto>(
                            cashFlowConfig.Configuration, JsonOptions);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex,
                            "Failed to deserialize configuration for {CashFlowType}",
                            cashFlowConfig.CashFlowType);
                        continue;
                    }

                    // Generate cash flows based on type
                    List<CashFlowItemInput> generatedCashFlows = await GenerateCashFlowsAsync(
                        cashFlowConfig.CashFlowType,
                        configDto,
                        loanDetail,
                        cancellationToken);

                    _logger.LogDebug(
                        "Generated {Count} cash flows for type {CashFlowType}",
                        generatedCashFlows.Count, cashFlowConfig.CashFlowType);

                    allCashFlows.AddRange(generatedCashFlows);
                }

                // Aggregate cash flows by month (sum amounts for same month)
                var aggregatedCashFlows = allCashFlows
                    .GroupBy(cf => cf.Month)
                    .Select(g => new CashFlowItemInput
                    {
                        Month = g.Key,
                        CashFlowAmount = g.Sum(cf => cf.CashFlowAmount)
                    })
                    .OrderBy(cf => cf.Month)
                    .ToList();

                _logger.LogInformation(
                    "Scenario {ScenarioName}: Aggregated to {Count} monthly cash flows",
                    scenario.ScenarioName, aggregatedCashFlows.Count);

                scenarioInputs.Add(new ScenarioCashFlowInput
                {
                    ScenarioId = scenario.Id,
                    ScenarioName = scenario.ScenarioName,
                    Probability = scenario.Probability / 100m, // Convert from percentage
                    CashFlows = aggregatedCashFlows
                });
            }

            // Validate total probability
            decimal totalProbability = scenarioInputs.Sum(s => s.Probability);
            if (Math.Abs(totalProbability - 1.0m) > 0.01m)
            {
                _logger.LogWarning(
                    "Scenario probabilities sum to {TotalProbability} instead of 1.00",
                    totalProbability);
            }

            return Result.Success(scenarioInputs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error building scenario cash flows for facility {FacilityNumber}",
                facilityNumber);

            return Result.Failure<List<ScenarioCashFlowInput>>(
                Error.Failure(
                    "CashFlowOrchestration.Error",
                    $"Error building scenario cash flows: {ex.Message}"));
        }
    }

    private async Task<List<CashFlowItemInput>> GenerateCashFlowsAsync(
        CashFlowsType cashFlowType,
        CashFlowConfigurationDto? config,
        FacilityLoanDetail loanDetail,
        CancellationToken cancellationToken)
    {
        return cashFlowType switch
        {
            CashFlowsType.ContractualCashFlows => await GenerateContractualCashFlowsAsync(
                config, loanDetail, cancellationToken),

            CashFlowsType.ContractModification => await GenerateModificationCashFlowsAsync(
                config, cancellationToken),

            CashFlowsType.CollateralRealization => GenerateCollateralCashFlows(config),

            CashFlowsType.LastQuarterCashFlows => await GenerateLastQuarterCashFlowsAsync(
                config, cancellationToken),

            CashFlowsType.OtherCashFlows => GenerateOtherCashFlows(config),

            _ => new List<CashFlowItemInput>()
        };
    }

    private async Task<List<CashFlowItemInput>> GenerateContractualCashFlowsAsync(
        CashFlowConfigurationDto? config,
        FacilityLoanDetail loanDetail,
        CancellationToken cancellationToken)
    {
        // Priority 1: If uploaded file exists, use it
        if (config?.UploadedFileId.HasValue == true)
        {
            List<CashFlowItemInput> parsedCashFlows = await ParseCashFlowsFromFileAsync(
                config.UploadedFileId.Value, cancellationToken);

            if (parsedCashFlows.Any())
            {
                _logger.LogInformation(
                    "Using uploaded payment schedule for contractual cash flows");
                return parsedCashFlows;
            }
        }

        // Priority 2: Generate from loan terms
        _logger.LogInformation(
            "Generating contractual cash flows from loan terms");

        int tenureMonths = _calculationService.CalculateTenureMonths(loanDetail.MaturityDate);
        List<MonthlyCashFlow> projections = _calculationService.GenerateCashFlowProjections(
            loanDetail.TotalOutstanding,
            loanDetail.InterestRate,
            tenureMonths,
            loanDetail.InstallmentType,
            DateTime.UtcNow);

        return projections.Select(cf => new CashFlowItemInput
        {
            Month = cf.Month,
            CashFlowAmount = cf.TotalAmount
        }).ToList();
    }

    private async Task<List<CashFlowItemInput>> GenerateModificationCashFlowsAsync(
        CashFlowConfigurationDto? config,
        CancellationToken cancellationToken)
    {
        if (config == null)
        {
            _logger.LogWarning("No configuration provided for contract modification");
            return new List<CashFlowItemInput>();
        }

        // Priority 1: If uploaded file exists, use it
        if (config.UploadedFileId.HasValue)
        {
            List<CashFlowItemInput> parsedCashFlows = await ParseCashFlowsFromFileAsync(
                config.UploadedFileId.Value, cancellationToken);

            if (parsedCashFlows.Any())
            {
                _logger.LogInformation(
                    "Using uploaded payment schedule for contract modification");
                return parsedCashFlows;
            }
        }

        // Priority 2: Use frequency/value/tenure parameters
        if (config.Frequency.HasValue && config.Value.HasValue && config.TenureMonths.HasValue)
        {
            _logger.LogInformation(
                "Generating modified cash flows from parameters: Frequency={Frequency}, Value={Value}, Tenure={Tenure}",
                config.Frequency.Value, config.Value.Value, config.TenureMonths.Value);

            var cashFlows = new List<CashFlowItemInput>();
            int frequencyMonths = config.Frequency.Value switch
            {
                PaymentFrequency.Monthly => 1,
                PaymentFrequency.Quarterly => 3,
                PaymentFrequency.SemiAnnually => 6,
                PaymentFrequency.Annually => 12,
                _ => 1
            };

            for (int month = frequencyMonths; month <= config.TenureMonths.Value; month += frequencyMonths)
            {
                cashFlows.Add(new CashFlowItemInput
                {
                    Month = month,
                    CashFlowAmount = config.Value.Value
                });
            }

            return cashFlows;
        }

        _logger.LogWarning(
            "No valid configuration found for contract modification (no file or parameters)");
        return new List<CashFlowItemInput>();
    }

    private List<CashFlowItemInput> GenerateCollateralCashFlows(
        CashFlowConfigurationDto? config)
    {
        if (config?.CollateralValue == null || config?.RealizationMonth == null)
        {
            _logger.LogWarning("No collateral configuration provided");
            return new List<CashFlowItemInput>();
        }

        decimal haircutPercentage = config.HaircutPercentage ?? 0.40m;
        decimal netRealizableValue = config.CollateralValue.Value * (1 - haircutPercentage);

        _logger.LogInformation(
            "Generating collateral cash flow: Value={CollateralValue}, Haircut={Haircut}%, Net={NetValue}, Month={Month}",
            config.CollateralValue.Value,
            haircutPercentage * 100,
            netRealizableValue,
            config.RealizationMonth.Value);

        return new List<CashFlowItemInput>
        {
            new()
            {
                Month = config.RealizationMonth.Value,
                CashFlowAmount = netRealizableValue
            }
        };
    }

    private async Task<List<CashFlowItemInput>> GenerateLastQuarterCashFlowsAsync(
        CashFlowConfigurationDto? config,
        CancellationToken cancellationToken)
    {
        if (config?.UploadedFileId == null)
        {
            _logger.LogWarning("No uploaded file ID provided for last quarter cash flows");
            return new List<CashFlowItemInput>();
        }

        List<CashFlowItemInput> cashFlows = await ParseCashFlowsFromFileAsync(
            config.UploadedFileId.Value, cancellationToken);

        _logger.LogInformation(
            "Parsed {Count} cash flows from last quarter file",
            cashFlows.Count);

        return cashFlows;
    }

    private static List<CashFlowItemInput> GenerateOtherCashFlows(
        CashFlowConfigurationDto? config)
    {
        if (config?.CustomCashFlows == null || !config.CustomCashFlows.Any())
        {
            return new List<CashFlowItemInput>();
        }

        return config.CustomCashFlows.Select(cf => new CashFlowItemInput
        {
            Month = cf.Month,
            CashFlowAmount = cf.Amount
        }).ToList();
    }

    private async Task<List<CashFlowItemInput>> ParseCashFlowsFromFileAsync(
        Guid uploadedFileId,
        CancellationToken cancellationToken)
    {
        UploadedFile? file = await _context.UploadedFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == uploadedFileId, cancellationToken);

        if (file == null)
        {
            _logger.LogWarning("Uploaded file not found: {FileId}", uploadedFileId);
            return new List<CashFlowItemInput>();
        }

        Result<List<ParsedCashFlow>> parseResult = await _excelParser.ParseCashFlowsAsync(
            file.PhysicalPath, cancellationToken);

        if (parseResult.IsFailure)
        {
            _logger.LogError(
                "Failed to parse cash flows from file {FileName}: {Error}",
                file.OriginalFileName, parseResult.Error.Description);
            return new List<CashFlowItemInput>();
        }

        return parseResult.Value.Select(cf => new CashFlowItemInput
        {
            Month = cf.Month,
            CashFlowAmount = cf.CashFlow
        }).ToList();
    }
}
