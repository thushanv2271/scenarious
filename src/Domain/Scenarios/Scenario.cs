using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedKernel;

namespace Domain.Scenarios;
public sealed class Scenario : Entity
{
    public Guid Id { get; set; }
    public Guid SegmentId { get; set; }
    public string ScenarioName { get; set; } = string.Empty;
    public decimal Probability { get; set; }
    public bool ContractualCashFlowsEnabled { get; set; }
    public bool LastQuarterCashFlowsEnabled { get; set; }
    public bool OtherCashFlowsEnabled { get; set; }
    public bool CollateralValueEnabled { get; set; }
    public Guid? UploadedFileId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
