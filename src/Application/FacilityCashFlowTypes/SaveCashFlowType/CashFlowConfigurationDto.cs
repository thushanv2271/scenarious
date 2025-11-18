using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.FacilityCashFlowTypes.SaveCashFlowType;
/// <summary>
/// Configuration data transfer object for cash flow type settings
/// </summary>
public sealed record CashFlowConfigurationDto
{
    /// <summary>
    /// Payment frequency for contract modifications (Monthly, Quarterly, Annually)
    /// </summary>
    public string? Frequency { get; init; }

    /// <summary>
    /// Payment value for contract modifications
    /// </summary>
    public decimal? Value { get; init; }

    /// <summary>
    /// Tenure in months for contract modifications
    /// </summary>
    public int? TenureMonths { get; init; }

    /// <summary>
    /// Collateral value for collateral realization
    /// </summary>
    public decimal? CollateralValue { get; init; }

    /// <summary>
    /// Month when collateral will be realized
    /// </summary>
    public int? RealizationMonth { get; init; }

    /// <summary>
    /// Description or notes
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Reference to uploaded file (for last quarter cash flows)
    /// </summary>
    public Guid? UploadedFileId { get; init; }

    /// <summary>
    /// Custom cash flows for other cash flow types
    /// </summary>
    public List<CustomCashFlowDto>? CustomCashFlows { get; init; }
}

