namespace Application.DTOs.LGDCalculation;

/// <summary>
/// Summary information for discounted cashflows by facility number
/// </summary>
public sealed record FacilityDiscountedCashflowSummary
{
    /// <summary>
    /// Gets the facility number
    /// </summary>
    public string FacilityNumber { get; init; } = string.Empty;

    /// <summary>
    /// Gets the total sum of discounted cashflows for this facility
    /// Returns null if facility is VC Closed (when isVCClosed = true)
    /// </summary>
    public decimal? TotalDiscountedCashflows { get; init; }

    /// <summary>
    /// Gets the number of records for this facility
    /// </summary>
    public int RecordCount { get; init; }

    /// <summary>
    /// Gets the minimum discounted cashflow value for this facility
    /// </summary>
    public decimal MinDiscountedCashflow { get; init; }

    /// <summary>
    /// Gets the maximum discounted cashflow value for this facility
    /// </summary>
    public decimal MaxDiscountedCashflow { get; init; }

    /// <summary>
    /// Gets the average discounted cashflow value for this facility
    /// </summary>
    public decimal AverageDiscountedCashflow { get; init; }

    /// <summary>
    /// Gets the maximum of Total Outstanding as at First NPL Date for this facility
    /// Returns null if facility is VC Closed (when isVCClosed = true)
    /// Replicates Excel formula: =IF([Facility Number]="","",MAX(MAXIFS([Total Outstanding as at First NPL Date],[Facility Number],[Facility Number]),0))
    /// </summary>
    public decimal? MaxTotalOutstandingAsAtFirstNplDate { get; init; }

    /// <summary>
    /// Gets the recovery rate for this facility as a percentage
    /// Set to 0 if facility is VC Closed (when isVCClosed = true)
    /// Replicates Excel formula: =IF([Facility Number]="","",IFERROR(IF([Sum of Discounted Cashflows]/[Max of Total Outstanding as at First NPL Date]>1,1,[Sum of Discounted Cashflows]/[Max of Total Outstanding as at First NPL Date]),0))
    /// Range: 0-100 where 100 = 100% recovery
    /// </summary>
    public decimal RecoveryRate { get; init; }

    /// <summary>
    /// Gets the LGD (Loss Given Default) for this facility as a percentage
    /// Set to 100 if facility is VC Closed (when isVCClosed = true)
    /// Calculated as: LGD = (1 - Recovery Rate) * 100
    /// Range: 0-100 where 100 = 100% loss
    /// </summary>
    public decimal Lgd { get; init; }

    /// <summary>
    /// Gets the calculated loss amount for this facility
    /// Returns null if facility is VC Closed (when isVCClosed = true)
    /// Replicates Excel formula: =IF([Facility Number]="","",[Max of Total Outstanding as at First NPL Date]*[LGD])
    /// </summary>
    public decimal? Loss { get; init; }

    /// <summary>
    /// Gets the number of years from NPL Date to Closure Date for this facility
    /// Replicates Excel formula: =IF([Facility Number]="","",MAX(ROUND(([Closure Date]-[First NPL Date])/365,0),0))
    /// </summary>
    public decimal YearsFromNplToClosureDate { get; init; }

    /// <summary>
    /// Gets the maximum Days Past Due (DPD) value for this facility
    /// Represents the highest DPD value found across all records for this facility
    /// Used in VC_LGD calculations to determine VC closed status
    /// </summary>
    public decimal MaxDaysPastDue { get; init; }

    /// <summary>
    /// Gets the VC closed status for this facility. Only available for VC_LGD calculations.
    /// Determines if the facility is considered closed based on maximum DPD vs VC_POINT threshold.
    /// Logic: If maximum DPD for the facility > (VC_POINT * 365), then true; otherwise false.
    /// Null for standard LGD calculations.
    /// </summary>
    public bool? IsVCClosed { get; init; }
}